using UnityEngine;
using UnityEngine.InputSystem;
using Survain.Core;
using Survain.Gameplay.Inventories;

namespace Survain.UI
{
    /// <summary>
    /// Pilote le panel inventaire (backpack 24 slots) : ouverture/fermeture via touche
    /// ToggleInventory (Tab + I), bind des slots à l'Inventory cible, gestion du curseur.
    ///
    /// Convention :
    ///  - Lit l'action 'ToggleInventory' de l'InputActionAsset partagé (map 'Player').
    ///    Ne touche pas au cycle Enable/Disable de la map (qui appartient à PlayerController).
    ///  - Quand le panel est ouvert, libère le curseur (lockState=None, visible=true).
    ///    À la fermeture, relock (Locked + invisible) pour rendre la main au PlayerCameraRig.
    ///  - Le jeu continue à tourner pendant que l'inventaire est ouvert (cohérent survie temps réel).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class InventoryUI : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Inventaire 'backpack' à afficher dans le panel.")]
        [SerializeField] private Inventory _backpack;

        [Header("Vues")]
        [Tooltip("GameObject du panel à toggler (typiquement un Canvas enfant).")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Vues de slots, dans l'ordre. Doit avoir exactement _backpack.Capacity éléments.")]
        [SerializeField] private InventorySlotView[] _slotViews;

        [Header("Input")]
        [Tooltip("Asset Input System partagé. L'action 'ToggleInventory' doit exister dans la map 'Player'.")]
        [SerializeField] private InputActionAsset _inputActions;

        private const string ActionMapName = "Player";
        private const string ToggleActionName = "ToggleInventory";

        private InputAction _toggleAction;
        private bool _isOpen;

        private void Awake()
        {
            if (_backpack == null || _panel == null || _slotViews == null || _inputActions == null)
            {
                SurvainLog.Error(SurvainLog.Category.UI,
                    "InventoryUI : _backpack, _panel, _slotViews ou _inputActions non assignés.", this);
                enabled = false;
                return;
            }

            if (_slotViews.Length != _backpack.Capacity)
            {
                SurvainLog.Error(SurvainLog.Category.UI,
                    $"InventoryUI : nombre de vues ({_slotViews.Length}) ≠ capacité backpack ({_backpack.Capacity}).", this);
                enabled = false;
                return;
            }

            var map = _inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            _toggleAction = map?.FindAction(ToggleActionName, throwIfNotFound: false);
            if (_toggleAction == null)
            {
                SurvainLog.Error(SurvainLog.Category.UI,
                    $"InventoryUI : action '{ToggleActionName}' introuvable dans la map '{ActionMapName}'.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_toggleAction != null) _toggleAction.performed += OnTogglePerformed;
        }

        private void OnDisable()
        {
            if (_toggleAction != null) _toggleAction.performed -= OnTogglePerformed;
        }

        private void Start()
        {
            for (int i = 0; i < _slotViews.Length; i++)
            {
                if (_slotViews[i] == null) continue;
                _slotViews[i].Bind(_backpack, i);
            }
            SetOpen(false);
        }

        /// <summary>Vrai quand le panneau backpack est ouvert.</summary>
        public bool IsOpen => _isOpen;

        private void OnTogglePerformed(InputAction.CallbackContext _)
        {
            // Si un coffre est ouvert, c'est ContainerUI qui possède la touche (fermeture) :
            // on ne toggle pas le backpack indépendamment pour éviter le double-handling.
            if (ContainerUI.Instance != null && ContainerUI.Instance.IsOpen) return;
            SetOpen(!_isOpen);
        }

        /// <summary>
        /// Ouvre/ferme le panneau backpack (libère/relock le curseur). Public car ContainerUI
        /// l'ouvre en même temps que le coffre pour permettre le drag entre les deux.
        /// </summary>
        public void SetOpen(bool open)
        {
            _isOpen = open;
            _panel.SetActive(open);

            if (open)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
