using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Survain.Core;
using Survain.Items;
using Survain.UI;

namespace Survain.Gameplay.Buildings
{
    /// <summary>
    /// Mode construction du joueur (issue #9). Posé sur _Player. Owner du mode : expose
    /// <see cref="IsActive"/> que PlayerHarvester lit pour se mettre en retrait (exclusion
    /// mutuelle du clic gauche : poser une structure vs frapper un nœud).
    ///
    /// Modèle « chantier » : poser une structure ne consomme PAS les ressources et n'en
    /// exige aucune — ça crée un ConstructionSite (fantôme qui réserve l'emplacement). Le
    /// joueur dépose ensuite les ressources dans le chantier (PlayerInteractor, touche E)
    /// jusqu'à complétion, où le bâtiment fini apparaît. Le placement valide ne dépend donc
    /// que de la faisabilité physique (pente + collision).
    ///
    /// Boucle :
    ///   - B (ToggleBuildMode) entre/sort du mode.
    ///   - En mode : raycast caméra → sol, position snappée à la grille, ghost translucide
    ///     vert (valide) / rouge (invalide). Validation = pente + collision.
    ///   - Clic gauche (Attack) pose le chantier (aucune ressource déduite ici).
    ///   - Q/E (RotateBuild) tourne par paliers de 90°.
    ///   - Molette (ou [ / ]) change la structure sélectionnée.
    ///   - Clic droit / Échap (CancelBuild) sort du mode.
    ///
    /// Le snap POC = arrondi à la grille (1 m) : deux bâtiments posés côte à côte
    /// s'alignent naturellement. Le snap par sockets est reporté au-delà du POC.
    ///
    /// Convention input : on lit les actions de la map "Player" sans toucher à son cycle
    /// Enable/Disable (PlayerController en est propriétaire — cf. CLAUDE.md 2026-04-26 §3).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildModeController : MonoBehaviour
    {
        [Header("Dépendances")]
        [Tooltip("Asset Input System partagé. La map 'Player' doit exposer ToggleBuildMode, RotateBuild, CancelBuild, SelectNextBuildable, SelectPrevBuildable et Attack.")]
        [SerializeField] private InputActionAsset _inputActions;

        [Tooltip("Transform de la caméra (origine du raycast de placement).")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Racine du joueur (ses colliders sont ignorés par le raycast et la collision).")]
        [SerializeField] private Transform _playerRoot;

        [Header("Catalogue")]
        [Tooltip("Bâtiments constructibles disponibles. On cycle entre eux à la molette (ou [ / ]).")]
        [SerializeField] private List<BuildingData> _buildables = new List<BuildingData>();

        [Header("Placement")]
        [Tooltip("Pas de la grille de snap, en mètres.")]
        [Min(0.25f)]
        [SerializeField] private float _gridSize = 1f;

        [Tooltip("Portée maximale du raycast de placement (mètres).")]
        [Range(1f, 30f)]
        [SerializeField] private float _maxPlaceDistance = 8f;

        [Tooltip("Pente maximale du sol autorisée pour poser (degrés). Au-delà = invalide.")]
        [Range(0f, 89f)]
        [SerializeField] private float _maxGroundSlope = 30f;

        [Header("Ghost")]
        [Tooltip("Couleur du ghost quand le placement est valide.")]
        [SerializeField] private Color _validColor = new Color(0.3f, 1f, 0.4f, 0.45f);

        [Tooltip("Couleur du ghost quand le placement est invalide.")]
        [SerializeField] private Color _invalidColor = new Color(1f, 0.3f, 0.3f, 0.45f);

        // ─── Constantes ─────────────────────────────────────────────────────

        private const string ActionMapName = "Player";

        // ─── État runtime ───────────────────────────────────────────────────

        private InputAction _toggleAction;
        private InputAction _rotateAction;
        private InputAction _cancelAction;
        private InputAction _nextAction;
        private InputAction _prevAction;
        private InputAction _attackAction;
        private InputAction _zoomAction; // molette : cycle de structure en mode build (zoom caméra gaté)

        private bool _isActive;
        private int _currentIndex;
        private float _yaw;

        private GameObject _ghostRoot;
        private Renderer[] _ghostRenderers;
        private Material _validMaterial;
        private Material _invalidMaterial;
        private bool _lastValidState;

        private BuildGridVisual _grid;

        private Vector3 _placePosition;
        private Vector3 _groundNormal = Vector3.up;
        private Collider _groundCollider;
        private bool _hasGround;
        private bool _placementValid;

        /// <summary>Vrai quand le mode construction est actif. Lu par PlayerHarvester.</summary>
        public bool IsActive => _isActive;

        private BuildingData CurrentBuildable =>
            (_buildables != null && _currentIndex >= 0 && _currentIndex < _buildables.Count)
                ? _buildables[_currentIndex]
                : null;

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_inputActions == null || _cameraTransform == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "BuildModeController : inputActions ou cameraTransform non assigné.", this);
                enabled = false;
                return;
            }

            if (_playerRoot == null) _playerRoot = transform;

            var map = _inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            _toggleAction = map?.FindAction("ToggleBuildMode", throwIfNotFound: false);
            _rotateAction = map?.FindAction("RotateBuild", throwIfNotFound: false);
            _cancelAction = map?.FindAction("CancelBuild", throwIfNotFound: false);
            _nextAction = map?.FindAction("SelectNextBuildable", throwIfNotFound: false);
            _prevAction = map?.FindAction("SelectPrevBuildable", throwIfNotFound: false);
            _attackAction = map?.FindAction("Attack", throwIfNotFound: false);
            _zoomAction = map?.FindAction("Zoom", throwIfNotFound: false);

            if (_toggleAction == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "BuildModeController : action 'ToggleBuildMode' introuvable dans la map 'Player'.", this);
                enabled = false;
                return;
            }

            BuildGhostMaterials();

            // Grille de placement, sur un GameObject enfant dédié.
            var gridGo = new GameObject("BuildGrid");
            gridGo.transform.SetParent(transform, false);
            _grid = gridGo.AddComponent<BuildGridVisual>();
            _grid.Build(extent: 15f, cellSize: _gridSize);
        }

        private void OnEnable()
        {
            if (_toggleAction != null) _toggleAction.performed += OnToggle;
            if (_rotateAction != null) _rotateAction.performed += OnRotate;
            if (_cancelAction != null) _cancelAction.performed += OnCancel;
            if (_nextAction != null) _nextAction.performed += OnSelectNext;
            if (_prevAction != null) _prevAction.performed += OnSelectPrev;
            if (_attackAction != null) _attackAction.started += OnPlace;
            if (_zoomAction != null) _zoomAction.performed += OnScroll;
        }

        private void OnDisable()
        {
            if (_toggleAction != null) _toggleAction.performed -= OnToggle;
            if (_rotateAction != null) _rotateAction.performed -= OnRotate;
            if (_cancelAction != null) _cancelAction.performed -= OnCancel;
            if (_nextAction != null) _nextAction.performed -= OnSelectNext;
            if (_prevAction != null) _prevAction.performed -= OnSelectPrev;
            if (_attackAction != null) _attackAction.started -= OnPlace;
            if (_zoomAction != null) _zoomAction.performed -= OnScroll;
        }

        private void Update()
        {
            if (!_isActive) return;
            UpdateGhostPlacement();
        }

        private void OnDestroy()
        {
            if (_isActive) InteractionPrompt.Instance.Hide();
        }

        // ─── Input handlers ─────────────────────────────────────────────────

        private void OnToggle(InputAction.CallbackContext _) => SetActive(!_isActive);

        private void OnCancel(InputAction.CallbackContext _)
        {
            if (_isActive) SetActive(false);
        }

        private void OnRotate(InputAction.CallbackContext ctx)
        {
            if (!_isActive) return;
            float v = ctx.ReadValue<float>();
            if (Mathf.Abs(v) < 0.5f) return; // ignore le retour à 0 (relâchement de touche)
            _yaw = Mathf.Repeat(_yaw + (v > 0f ? 90f : -90f), 360f);
        }

        private void OnSelectNext(InputAction.CallbackContext _) => CycleBuildable(1);
        private void OnSelectPrev(InputAction.CallbackContext _) => CycleBuildable(-1);

        private void OnScroll(InputAction.CallbackContext ctx)
        {
            // En mode build, la molette change de structure (le zoom caméra est gaté
            // côté PlayerCameraRig via IsActive). Hors mode, ce handler ne fait rien.
            if (!_isActive) return;
            float delta = ctx.ReadValue<float>();
            if (Mathf.Abs(delta) < 0.01f) return;
            CycleBuildable(delta > 0f ? 1 : -1);
        }

        private void OnPlace(InputAction.CallbackContext _)
        {
            if (!_isActive) return;
            TryPlace();
        }

        // ─── Mode ───────────────────────────────────────────────────────────

        private void SetActive(bool active)
        {
            if (_isActive == active) return;

            if (active && (_buildables == null || _buildables.Count == 0))
            {
                SurvainLog.Warn(SurvainLog.Category.Gameplay,
                    "BuildModeController : aucune structure dans le catalogue, mode construction ignoré.", this);
                return;
            }

            _isActive = active;

            if (_isActive)
            {
                _currentIndex = Mathf.Clamp(_currentIndex, 0, _buildables.Count - 1);
                BuildGhost();
                if (_grid != null) _grid.SetVisible(true);
                SurvainLog.Info(SurvainLog.Category.Gameplay, "Mode construction : ON.", this);
            }
            else
            {
                DestroyGhost();
                if (_grid != null) _grid.SetVisible(false);
                InteractionPrompt.Instance.Hide();
                SurvainLog.Info(SurvainLog.Category.Gameplay, "Mode construction : OFF.", this);
            }
        }

        private void CycleBuildable(int direction)
        {
            if (!_isActive || _buildables.Count == 0) return;
            _currentIndex = (int)Mathf.Repeat(_currentIndex + direction, _buildables.Count);
            BuildGhost();
        }

        // ─── Ghost ──────────────────────────────────────────────────────────

        private void BuildGhost()
        {
            DestroyGhost();

            var data = CurrentBuildable;
            if (data == null) return;

            _ghostRoot = new GameObject($"BuildGhost_{data.Id}");
            _ghostRoot.transform.SetParent(null);
            BuildingVisualFactory.Create(data, _ghostRoot.transform);

            // Le ghost ne doit ni bloquer le raycast de placement ni être détecté par le
            // test de collision : on désactive tous ses colliders.
            var colliders = _ghostRoot.GetComponentsInChildren<Collider>(includeInactive: true);
            foreach (var col in colliders) col.enabled = false;

            _ghostRenderers = _ghostRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
            _lastValidState = true;
            ApplyGhostColor(true);
        }

        private void DestroyGhost()
        {
            if (_ghostRoot != null) Destroy(_ghostRoot);
            _ghostRoot = null;
            _ghostRenderers = null;
        }

        private void UpdateGhostPlacement()
        {
            var data = CurrentBuildable;
            if (data == null || _ghostRoot == null) return;

            _hasGround = TryRaycastGround(out var hitPoint, out _groundNormal, out _groundCollider);

            if (!_hasGround)
            {
                // Pas de sol en vue : on cache le ghost sous le décor (le garder visible
                // flottant serait trompeur). On le pousse loin et on marque invalide.
                _ghostRoot.SetActive(false);
                _placementValid = false;
                InteractionPrompt.Instance.Show($"{data.DisplayName} — visez le sol");
                return;
            }

            if (!_ghostRoot.activeSelf) _ghostRoot.SetActive(true);

            _placePosition = SnapToGrid(hitPoint);
            _ghostRoot.transform.SetPositionAndRotation(
                _placePosition, Quaternion.Euler(0f, _yaw, 0f));

            if (_grid != null) _grid.SetCenter(_placePosition);

            _placementValid = ValidatePlacement(data, _placePosition, _groundNormal, out string reason);
            if (_placementValid != _lastValidState)
            {
                _lastValidState = _placementValid;
                ApplyGhostColor(_placementValid);
            }

            InteractionPrompt.Instance.Show(
                _placementValid
                    ? $"[Clic gauche] Poser chantier {data.DisplayName} ({CostLabel(data)})   ·   Q/E tourner   ·   molette changer"
                    : $"{data.DisplayName} — {reason}   ·   molette changer");
        }

        /// <summary>Libellé compact du coût d'un bâtiment, ex : "8 Bois brut, 4 Pierre brute".</summary>
        private static string CostLabel(BuildingData data)
        {
            var cost = data.Cost;
            if (cost == null || cost.Length == 0) return "gratuit";
            var parts = new List<string>(cost.Length);
            foreach (var c in cost)
            {
                if (c.Item == null) continue;
                string label = string.IsNullOrEmpty(c.Item.DisplayName) ? c.Item.Id : c.Item.DisplayName;
                parts.Add($"{c.Amount} {label}");
            }
            return parts.Count > 0 ? string.Join(", ", parts) : "gratuit";
        }

        private void ApplyGhostColor(bool valid)
        {
            if (_ghostRenderers == null) return;
            var mat = valid ? _validMaterial : _invalidMaterial;
            foreach (var rend in _ghostRenderers)
            {
                if (rend != null) rend.sharedMaterial = mat;
            }
        }

        // ─── Raycast / snap / validation ────────────────────────────────────

        private bool TryRaycastGround(out Vector3 point, out Vector3 normal, out Collider ground)
        {
            point = Vector3.zero;
            normal = Vector3.up;
            ground = null;

            var hits = Physics.RaycastAll(
                _cameraTransform.position, _cameraTransform.forward,
                _maxPlaceDistance, ~0, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                var t = hits[i].collider.transform;
                if (t == _playerRoot || t.IsChildOf(_playerRoot)) continue;

                point = hits[i].point;
                normal = hits[i].normal;
                ground = hits[i].collider;
                return true;
            }
            return false;
        }

        private Vector3 SnapToGrid(Vector3 worldPoint)
        {
            float x = Mathf.Round(worldPoint.x / _gridSize) * _gridSize;
            float z = Mathf.Round(worldPoint.z / _gridSize) * _gridSize;
            return new Vector3(x, worldPoint.y, z);
        }

        private bool ValidatePlacement(BuildingData data, Vector3 pos, Vector3 normal, out string reason)
        {
            // 1. Pente du sol.
            float slope = Vector3.Angle(normal, Vector3.up);
            if (slope > _maxGroundSlope)
            {
                reason = "terrain trop pentu";
                return false;
            }

            // 2. Collision avec un autre objet (hors joueur, hors sol porteur, hors ghost).
            // Le modèle chantier ne vérifie PAS les ressources à la pose : on pose le plan
            // librement, l'appro se fait ensuite via le dépôt dans le chantier.
            if (IsObstructed(data, pos))
            {
                reason = "emplacement obstrué";
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private bool IsObstructed(BuildingData data, Vector3 pos)
        {
            var size = data.Size;
            // Boîte légèrement rétrécie, posée au-dessus du pivot sol (évite de mordre le terrain).
            Vector3 center = pos + Vector3.up * (size.y * 0.5f);
            Vector3 halfExtents = size * 0.45f;
            var orientation = Quaternion.Euler(0f, _yaw, 0f);

            var overlaps = Physics.OverlapBox(
                center, halfExtents, orientation, ~0, QueryTriggerInteraction.Ignore);

            foreach (var col in overlaps)
            {
                if (col == _groundCollider) continue;
                var t = col.transform;
                if (t == _playerRoot || t.IsChildOf(_playerRoot)) continue;
                if (_ghostRoot != null && (t == _ghostRoot.transform || t.IsChildOf(_ghostRoot.transform))) continue;
                return true; // un obstacle réel
            }
            return false;
        }

        // ─── Pose ───────────────────────────────────────────────────────────

        private void TryPlace()
        {
            var data = CurrentBuildable;
            if (data == null || !_hasGround || !_placementValid) return;

            // Modèle chantier : on instancie un ConstructionSite (pas le bâtiment fini).
            // Aucune ressource n'est consommée ici ; elles seront déposées dans le chantier.
            var go = new GameObject($"ConstructionSite_{data.Id}");
            go.transform.position = _placePosition;
            var site = go.AddComponent<ConstructionSite>();
            site.Initialize(data, Quaternion.Euler(0f, _yaw, 0f));

            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Chantier '{data.Id}' posé en {_placePosition}.", this);
        }

        // ─── Materials ──────────────────────────────────────────────────────

        private void BuildGhostMaterials()
        {
            _validMaterial = CreateGhostMaterial(_validColor);
            _invalidMaterial = CreateGhostMaterial(_invalidColor);
        }

        private static Material CreateGhostMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);

            mat.SetFloat("_Surface", 1f); // transparent
            mat.SetFloat("_Blend", 0f);   // alpha blend
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.SetColor("_BaseColor", color);
            mat.color = color;
            return mat;
        }
    }
}
