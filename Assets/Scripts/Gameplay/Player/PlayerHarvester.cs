using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Survain.Core;
using Survain.Gameplay.Buildings;
using Survain.Gameplay.Items;
using Survain.UI;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Système de récolte côté joueur. Lit les actions Interact / Previous / Next
    /// de l'InputActionAsset (sans toucher au cycle Enable/Disable de la map :
    /// c'est PlayerController qui en est propriétaire — cf. CLAUDE.md 2026-04-26 §3).
    ///
    /// Mécanique (D2 clic discret) : à chaque pression sur Interact, raycast depuis
    /// la caméra vers le pointer center. Si on touche un ResourceNode à portée,
    /// on tente TryHit() avec l'outil courant de PlayerEquipment. Punch caméra à chaque coup.
    ///
    /// Cooldown entre 2 coups : HarvestSeconds du nœud / HarvestSpeed de l'outil.
    /// Les touches Previous/Next basculent entre les slots de PlayerEquipment.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHarvester : MonoBehaviour
    {
        [Header("Dépendances")]
        [Tooltip("Asset Input System partagé. La map 'Player' doit exposer Interact, Previous, Next.")]
        [SerializeField] private InputActionAsset _inputActions;

        [Tooltip("Transform de la caméra (origine du raycast).")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Rig caméra pour le punch feedback (optionnel).")]
        [SerializeField] private PlayerCameraRig _cameraRig;

        [Tooltip("Équipement joueur (source de l'outil courant).")]
        [SerializeField] private PlayerEquipment _equipment;

        [Tooltip("Racine du joueur (ses colliders seront ignorés par le raycast). Si null = self.")]
        [SerializeField] private Transform _playerRoot;

        [Tooltip("Mode construction (optionnel). Quand il est actif, la récolte est suspendue : le clic gauche sert à poser une structure, pas à frapper un nœud.")]
        [SerializeField] private BuildModeController _buildMode;

        [Header("Récolte")]
        [Tooltip("Portée maximum du raycast de récolte (mètres).")]
        [Range(1f, 20f)]
        [SerializeField] private float _maxReach = 6f;

        [Tooltip("Layers ignorés par le raycast de récolte. Le terrain doit en faire partie pour éviter de bloquer les nœuds derrière une pente.")]
        [SerializeField] private LayerMask _harvestRaycastMask = ~0;

        [Header("Feedback")]
        [Tooltip("Intensité du punch caméra (degrés) à chaque coup. 0 = désactivé.")]
        [Range(0f, 10f)]
        [SerializeField] private float _cameraPunchDegrees = 2f;

        // ─── Events ─────────────────────────────────────────────────────────

        /// <summary>
        /// Déclenché à chaque coup qui touche effectivement un nœud (TryHit a réussi),
        /// AVANT l'application du cooldown. Consommé par PlayerVisualAnimator pour
        /// déclencher l'anim de récolte (punch générique au Sprint 1).
        /// </summary>
        public event Action HitLanded;

        // ─── Constantes ─────────────────────────────────────────────────────

        private const string ActionMapName = "Player";
        private const string AttackActionName = "Attack";
        private const int HotbarSize = 4;
        private static readonly string[] EquipSlotActionNames =
            { "EquipSlot1", "EquipSlot2", "EquipSlot3", "EquipSlot4" };

        // ─── État runtime ───────────────────────────────────────────────────

        private InputAction _attackAction;
        private InputAction[] _equipSlotActions;
        private Action<InputAction.CallbackContext>[] _equipSlotHandlers;
        private float _nextHitAllowedAt; // Time.time minimal pour le prochain coup
        private ResourceNode _currentTarget; // résultat du dernier raycast hover

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_inputActions == null || _cameraTransform == null || _equipment == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerHarvester : inputActions, cameraTransform ou equipment non assigné.", this);
                enabled = false;
                return;
            }

            if (_playerRoot == null) _playerRoot = transform;

            var map = _inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            _attackAction = map?.FindAction(AttackActionName, throwIfNotFound: false);

            _equipSlotActions = new InputAction[HotbarSize];
            _equipSlotHandlers = new Action<InputAction.CallbackContext>[HotbarSize];
            for (int i = 0; i < HotbarSize; i++)
            {
                _equipSlotActions[i] = map?.FindAction(EquipSlotActionNames[i], throwIfNotFound: false);
                int slotIndex = i; // capture pour le delegate
                _equipSlotHandlers[i] = _ => _equipment.SetTool(slotIndex);
            }

            if (_attackAction == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerHarvester : action 'Attack' introuvable dans la map 'Player'.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < HotbarSize; i++)
            {
                if (_equipSlotActions[i] == null)
                {
                    SurvainLog.Warn(SurvainLog.Category.Gameplay,
                        $"PlayerHarvester : action '{EquipSlotActionNames[i]}' introuvable. Le slot hotbar {i} ne sera pas équipable au clavier.",
                        this);
                }
            }
        }

        private void OnEnable()
        {
            // On utilise 'started' (pression initiale) plutôt que 'performed' pour bypass
            // les interactions custom potentielles (Hold, Tap, etc.) qui ajouteraient un délai.
            if (_attackAction != null) _attackAction.started += OnAttackStarted;

            if (_equipSlotActions != null)
            {
                for (int i = 0; i < HotbarSize; i++)
                {
                    if (_equipSlotActions[i] != null) _equipSlotActions[i].performed += _equipSlotHandlers[i];
                }
            }
        }

        private void OnDisable()
        {
            if (_attackAction != null) _attackAction.started -= OnAttackStarted;

            if (_equipSlotActions != null)
            {
                for (int i = 0; i < HotbarSize; i++)
                {
                    if (_equipSlotActions[i] != null) _equipSlotActions[i].performed -= _equipSlotHandlers[i];
                }
            }
        }

        // ─── Update : hover & prompt ────────────────────────────────────────

        private void Update()
        {
            // En mode construction, la récolte est suspendue (clic gauche = poser).
            // On lâche la cible courante et le prompt pour ne pas laisser de surbrillance.
            if (_buildMode != null && _buildMode.IsActive)
            {
                if (_currentTarget != null)
                {
                    _currentTarget.SetHighlighted(false);
                    _currentTarget = null;
                    InteractionPrompt.Instance.Hide();
                }
                return;
            }

            // Met à jour la cible courante via raycast continu.
            var target = RaycastForNode();
            if (target != _currentTarget)
            {
                if (_currentTarget != null) _currentTarget.SetHighlighted(false);
                _currentTarget = target;
                if (_currentTarget != null) _currentTarget.SetHighlighted(true);
                UpdatePrompt();
            }
        }

        private void OnDestroy()
        {
            // Sécurité : si le harvester disparaît, on cache le prompt résiduel
            // et on retire la surbrillance du nœud visé.
            if (_currentTarget != null)
            {
                _currentTarget.SetHighlighted(false);
                InteractionPrompt.Instance.Hide();
            }
        }

        private void UpdatePrompt()
        {
            if (_currentTarget == null)
            {
                InteractionPrompt.Instance.Hide();
                return;
            }
            InteractionPrompt.Instance.Show($"[Clic gauche] Récolter {_currentTarget.Data.DisplayName}");
        }

        // ─── Input handlers ─────────────────────────────────────────────────

        private void OnAttackStarted(InputAction.CallbackContext _) => TryHarvest();

        // ─── Logique de récolte ─────────────────────────────────────────────

        /// <summary>
        /// Raycast caméra + tri par distance + filtre joueur. Retourne le ResourceNode
        /// le plus proche dans la portée, ou null si rien d'interactable.
        /// </summary>
        private ResourceNode RaycastForNode()
        {
            // Portée mesurée DEPUIS LE JOUEUR (cf. PlayerInteractor) : on ajoute la distance
            // caméra→joueur pour que _maxReach reste la vraie portée devant le perso, quel que soit le zoom.
            float camToPlayer = Vector3.Distance(_cameraTransform.position, _playerRoot.position);
            var hits = Physics.RaycastAll(
                _cameraTransform.position, _cameraTransform.forward,
                camToPlayer + _maxReach, _harvestRaycastMask, QueryTriggerInteraction.Ignore);

            if (hits.Length == 0) return null;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            for (int i = 0; i < hits.Length; i++)
            {
                var t = hits[i].collider.transform;
                if (t == _playerRoot || t.IsChildOf(_playerRoot)) continue;

                // Premier hit non-joueur. Soit c'est un nœud récoltable, soit la vue est
                // obstruée par autre chose (terrain, drop, mur...) — dans ce cas, pas de cible.
                var node = hits[i].collider.GetComponentInParent<ResourceNode>();
                if (node != null && !node.IsDepleted) return node;
                return null;
            }
            return null;
        }

        private void TryHarvest()
        {
            // Le clic gauche est consommé par le mode construction quand il est actif.
            if (_buildMode != null && _buildMode.IsActive) return;
            if (Time.time < _nextHitAllowedAt) return;

            var node = _currentTarget;
            if (node == null || node.IsDepleted) return;

            var tool = _equipment.CurrentTool;
            bool hitLanded = node.TryHit(tool);
            if (!hitLanded) return;

            HitLanded?.Invoke();

            // Cooldown jusqu'au prochain coup
            float speed = (tool != null && tool.HarvestSpeed > 0f) ? tool.HarvestSpeed : 1f;
            float cooldown = node.Data.HarvestSeconds / speed;
            _nextHitAllowedAt = Time.time + cooldown;

            // Punch caméra (juice). Pitch positif = caméra "recule" vers le haut.
            if (_cameraRig != null && _cameraPunchDegrees > 0f)
            {
                _cameraRig.Punch(_cameraPunchDegrees);
            }
        }
    }
}
