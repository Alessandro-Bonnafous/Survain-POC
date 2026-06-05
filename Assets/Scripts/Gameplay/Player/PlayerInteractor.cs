using UnityEngine;
using UnityEngine.InputSystem;
using Survain.Core;
using Survain.Gameplay.Buildings;
using Survain.Gameplay.Interaction;
using Survain.Gameplay.Inventories;
using Survain.UI;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Interaction générique du joueur (touche E). Vise un IInteractable avec la caméra,
    /// affiche son prompt, et déclenche son Interact sur E — dépôt dans un chantier, ouverture
    /// d'un coffre, plus tard dialogue PNJ / portes. Un seul interacteur pour toutes les cibles
    /// (évite la multiplication d'interacteurs concurrents sur la même touche).
    ///
    /// Même pattern que PlayerHarvester (raycast caméra + surbrillance + prompt) ; suspendu
    /// quand le mode construction est actif (le prompt de pose prime alors). Ne touche pas au
    /// cycle Enable/Disable de la map "Player" (propriété de PlayerController).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [Header("Dépendances")]
        [Tooltip("Asset Input System partagé. La map 'Player' doit exposer 'Interact'.")]
        [SerializeField] private InputActionAsset _inputActions;

        [Tooltip("Transform de la caméra (origine du raycast).")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Racine du joueur (ses colliders sont ignorés par le raycast). Si null = self.")]
        [SerializeField] private Transform _playerRoot;

        [Tooltip("Sac à dos : inventaire de l'acteur passé aux interactions (dépôt chantier, transferts coffre).")]
        [SerializeField] private Inventory _backpack;

        [Tooltip("Mode construction (optionnel). Quand il est actif, l'interaction est suspendue.")]
        [SerializeField] private BuildModeController _buildMode;

        [Header("Interaction")]
        [Tooltip("Portée maximale du raycast d'interaction (mètres).")]
        [Range(1f, 20f)]
        [SerializeField] private float _maxReach = 6f;

        private const string ActionMapName = "Player";
        private const string InteractActionName = "Interact";

        private InputAction _interactAction;
        private IInteractable _currentTarget;

        private void Awake()
        {
            if (_inputActions == null || _cameraTransform == null || _backpack == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerInteractor : inputActions, cameraTransform ou backpack non assigné.", this);
                enabled = false;
                return;
            }

            if (_playerRoot == null) _playerRoot = transform;

            var map = _inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            _interactAction = map?.FindAction(InteractActionName, throwIfNotFound: false);
            if (_interactAction == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    $"PlayerInteractor : action '{InteractActionName}' introuvable dans la map '{ActionMapName}'.", this);
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

            var target = RaycastForInteractable();
            if (!ReferenceEquals(target, _currentTarget))
            {
                if (_currentTarget != null) _currentTarget.SetHighlighted(false);
                _currentTarget = target;
                if (_currentTarget != null) _currentTarget.SetHighlighted(true);
                UpdatePrompt();
            }
            else if (_currentTarget != null)
            {
                UpdatePrompt(); // rafraîchit le prompt (ex. décompte de progression d'un chantier)
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
            InteractionPrompt.Instance.Show(_currentTarget.GetInteractionPrompt());
        }

        private void OnInteract(InputAction.CallbackContext _)
        {
            if (_buildMode != null && _buildMode.IsActive) return;
            if (_currentTarget == null || !_currentTarget.IsInteractable) return;

            _currentTarget.Interact(_backpack);

            // La cible peut être devenue inactive (chantier terminé) → on réévalue.
            if (_currentTarget == null || !_currentTarget.IsInteractable)
            {
                _currentTarget = null;
                InteractionPrompt.Instance.Hide();
            }
            else
            {
                UpdatePrompt();
            }
        }

        private IInteractable RaycastForInteractable()
        {
            // Portée mesurée DEPUIS LE JOUEUR : en 3e personne la caméra est derrière le perso,
            // donc on ajoute la distance caméra→joueur pour que _maxReach soit la vraie portée
            // devant le perso, indépendamment du zoom (sinon dézoomer « mange » la portée).
            float camToPlayer = Vector3.Distance(_cameraTransform.position, _playerRoot.position);
            var hits = Physics.RaycastAll(
                _cameraTransform.position, _cameraTransform.forward,
                camToPlayer + _maxReach, ~0, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) return null;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                var t = hits[i].collider.transform;
                if (t == _playerRoot || t.IsChildOf(_playerRoot)) continue;

                var interactable = hits[i].collider.GetComponentInParent<IInteractable>();
                if (interactable != null && interactable.IsInteractable) return interactable;
                return null; // premier hit non-joueur n'est pas interactable → vue obstruée
            }
            return null;
        }
    }
}
