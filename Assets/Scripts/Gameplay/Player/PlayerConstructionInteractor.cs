using UnityEngine;
using UnityEngine.InputSystem;
using Survain.Core;
using Survain.Gameplay.Buildings;
using Survain.Gameplay.Inventories;
using Survain.UI;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Interaction du joueur avec les chantiers (ConstructionSite). Vise un chantier avec
    /// la caméra, appuie sur E (action Interact) → dépose dans le chantier les ressources
    /// correspondantes du sac à dos. Quand le chantier est complet, il se transforme tout
    /// seul en bâtiment fini.
    ///
    /// Même pattern que PlayerHarvester (raycast caméra + surbrillance + prompt) ; suspendu
    /// quand le mode construction est actif (le prompt de pose prime alors). L'action E est
    /// la touche d'interaction générique réservée (cf. CLAUDE.md 2026-05-22 §2) — dépôt de
    /// chantier aujourd'hui, dialogue/ouverture demain.
    ///
    /// Ne touche pas au cycle Enable/Disable de la map "Player" (PlayerController en est
    /// propriétaire).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerConstructionInteractor : MonoBehaviour
    {
        [Header("Dépendances")]
        [Tooltip("Asset Input System partagé. La map 'Player' doit exposer 'Interact'.")]
        [SerializeField] private InputActionAsset _inputActions;

        [Tooltip("Transform de la caméra (origine du raycast).")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Racine du joueur (ses colliders sont ignorés par le raycast). Si null = self.")]
        [SerializeField] private Transform _playerRoot;

        [Tooltip("Sac à dos : source des ressources déposées dans le chantier.")]
        [SerializeField] private Inventory _backpack;

        [Tooltip("Mode construction (optionnel). Quand il est actif, l'interaction chantier est suspendue.")]
        [SerializeField] private BuildModeController _buildMode;

        [Header("Interaction")]
        [Tooltip("Portée maximale du raycast de dépôt (mètres).")]
        [Range(1f, 20f)]
        [SerializeField] private float _maxReach = 6f;

        private const string ActionMapName = "Player";
        private const string InteractActionName = "Interact";

        private InputAction _interactAction;
        private ConstructionSite _currentTarget;

        private void Awake()
        {
            if (_inputActions == null || _cameraTransform == null || _backpack == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerConstructionInteractor : inputActions, cameraTransform ou backpack non assigné.", this);
                enabled = false;
                return;
            }

            if (_playerRoot == null) _playerRoot = transform;

            var map = _inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            _interactAction = map?.FindAction(InteractActionName, throwIfNotFound: false);
            if (_interactAction == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    $"PlayerConstructionInteractor : action '{InteractActionName}' introuvable dans la map '{ActionMapName}'.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_interactAction != null) _interactAction.started += OnInteract;
        }

        private void OnDisable()
        {
            if (_interactAction != null) _interactAction.started -= OnInteract;
        }

        private void Update()
        {
            // Suspendu en mode construction (le prompt de pose prime).
            if (_buildMode != null && _buildMode.IsActive)
            {
                ClearTarget();
                return;
            }

            var target = RaycastForSite();
            if (target != _currentTarget)
            {
                if (_currentTarget != null) _currentTarget.SetHighlighted(false);
                _currentTarget = target;
                if (_currentTarget != null) _currentTarget.SetHighlighted(true);
                UpdatePrompt(); // shows si cible, masque si null (transition)
            }
            else if (_currentTarget != null)
            {
                UpdatePrompt(); // rafraîchit le décompte de progression chaque frame
            }
        }

        private void OnDestroy()
        {
            if (_currentTarget != null)
            {
                _currentTarget.SetHighlighted(false);
                InteractionPrompt.Instance.Hide();
            }
        }

        private void ClearTarget()
        {
            if (_currentTarget == null) return;
            _currentTarget.SetHighlighted(false);
            _currentTarget = null;
            InteractionPrompt.Instance.Hide();
        }

        private void UpdatePrompt()
        {
            if (_currentTarget == null)
            {
                InteractionPrompt.Instance.Hide();
                return;
            }
            InteractionPrompt.Instance.Show($"[E] Construire — {_currentTarget.ProgressLabel()}");
        }

        private void OnInteract(InputAction.CallbackContext _)
        {
            if (_buildMode != null && _buildMode.IsActive) return;
            if (_currentTarget == null || _currentTarget.IsComplete) return;

            int deposited = _currentTarget.Deposit(_backpack);
            if (deposited <= 0)
            {
                SurvainLog.Info(SurvainLog.Category.Gameplay,
                    "Aucune ressource à déposer (sac vide pour ce chantier ?).", this);
                return;
            }

            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Dépôt de {deposited} ressource(s) dans le chantier.", this);

            // Le chantier peut s'être complété (et détruit) pendant le dépôt → on rafraîchit.
            if (_currentTarget == null || _currentTarget.IsComplete)
            {
                _currentTarget = null;
                InteractionPrompt.Instance.Hide();
            }
            else
            {
                UpdatePrompt();
            }
        }

        private ConstructionSite RaycastForSite()
        {
            var hits = Physics.RaycastAll(
                _cameraTransform.position, _cameraTransform.forward,
                _maxReach, ~0, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) return null;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                var t = hits[i].collider.transform;
                if (t == _playerRoot || t.IsChildOf(_playerRoot)) continue;

                var site = hits[i].collider.GetComponentInParent<ConstructionSite>();
                if (site != null && !site.IsComplete) return site;
                return null; // premier hit non-joueur n'est pas un chantier → vue obstruée
            }
            return null;
        }
    }
}
