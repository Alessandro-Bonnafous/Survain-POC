using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Survain.Core;
using Survain.Gameplay.Inventories;

namespace Survain.UI
{
    /// <summary>
    /// Panneau d'un conteneur (coffre) ouvert par le joueur. Affiche les slots du conteneur
    /// et ouvre simultanément le panneau backpack (via InventoryUI) pour permettre le
    /// drag & drop entre les deux — le InventoryDragController gère déjà le transfert
    /// inter-conteneurs (SwapAcross), donc aucun code de transfert spécifique ici.
    ///
    /// Singleton lazy (comme InventoryDragController) : StorageContainer.Interact appelle
    /// ContainerUI.Instance.Open(inventaireDuCoffre, libellé).
    ///
    /// Les vues de slots sont clonées à partir d'un template au démarrage (peu de setup
    /// manuel : un seul slot à fabriquer, comme pour le backpack #7 mais cloné en code).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ContainerUI : MonoBehaviour
    {
        public static ContainerUI Instance { get; private set; }

        [Header("Vues")]
        [Tooltip("Panneau racine du conteneur (activé/désactivé à l'ouverture/fermeture).")]
        [SerializeField] private GameObject _panel;

        [Tooltip("Slot template à cloner (un InventorySlotView fabriqué une fois). Désactivé au runtime.")]
        [SerializeField] private InventorySlotView _slotTemplate;

        [Tooltip("Parent des slots clonés (idéalement un GridLayoutGroup).")]
        [SerializeField] private Transform _slotsParent;

        [Tooltip("Libellé du conteneur (nom du coffre). Facultatif.")]
        [SerializeField] private Text _titleLabel;

        [Tooltip("Nombre max de slots affichables (≥ capacité du plus grand conteneur).")]
        [Min(1)]
        [SerializeField] private int _maxSlots = 24;

        [Header("Coordination")]
        [Tooltip("Panneau backpack à ouvrir en même temps que le coffre (pour drag entre les deux).")]
        [SerializeField] private InventoryUI _inventoryUI;

        [Header("Input")]
        [Tooltip("Asset Input System partagé. L'action 'ToggleInventory' ferme le coffre.")]
        [SerializeField] private InputActionAsset _inputActions;

        private const string ActionMapName = "Player";
        private const string ToggleActionName = "ToggleInventory";

        private InputAction _toggleAction;
        private InventorySlotView[] _slots;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                SurvainLog.Warn(SurvainLog.Category.UI,
                    "ContainerUI : instance multiple détectée. Destruction du doublon.", this);
                Destroy(this);
                return;
            }
            Instance = this;

            if (_panel == null || _slotTemplate == null || _slotsParent == null || _inputActions == null)
            {
                SurvainLog.Error(SurvainLog.Category.UI,
                    "ContainerUI : _panel, _slotTemplate, _slotsParent ou _inputActions non assignés.", this);
                enabled = false;
                return;
            }

            var map = _inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            _toggleAction = map?.FindAction(ToggleActionName, throwIfNotFound: false);

            BuildSlots();
            _panel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            if (_toggleAction != null) _toggleAction.performed += OnToggle;
        }

        private void OnDisable()
        {
            if (_toggleAction != null) _toggleAction.performed -= OnToggle;
        }

        private void BuildSlots()
        {
            _slots = new InventorySlotView[_maxSlots];
            for (int i = 0; i < _maxSlots; i++)
            {
                var clone = Instantiate(_slotTemplate, _slotsParent);
                clone.gameObject.name = $"ContainerSlot_{i}";
                clone.gameObject.SetActive(true);
                _slots[i] = clone;
            }
            _slotTemplate.gameObject.SetActive(false);
        }

        /// <summary>
        /// Ouvre le panneau sur l'inventaire d'un conteneur. Bind les slots et ouvre aussi
        /// le backpack pour le drag entre les deux.
        /// </summary>
        public void Open(Inventory container, string label)
        {
            if (container == null) return;

            for (int i = 0; i < _slots.Length; i++)
            {
                bool used = i < container.Capacity;
                _slots[i].gameObject.SetActive(used);
                if (used) _slots[i].Bind(container, i);
            }

            if (_titleLabel != null) _titleLabel.text = string.IsNullOrEmpty(label) ? "Coffre" : label;

            _panel.SetActive(true);
            // Le backpack s'ouvre avec le coffre (drag inter-conteneur) ; InventoryUI route déjà
            // vers UiMode. Sans backpack, on pousse le mode UI directement.
            if (_inventoryUI != null) _inventoryUI.SetOpen(true);
            else UiMode.Push();

            _isOpen = true;
        }

        /// <summary>Ferme le panneau (et le backpack ouvert avec lui).</summary>
        public void Close()
        {
            if (!_isOpen) return;
            _panel.SetActive(false);
            if (_inventoryUI != null) _inventoryUI.SetOpen(false);
            else UiMode.Pop();
            _isOpen = false;
        }

        private void OnToggle(InputAction.CallbackContext _)
        {
            if (_isOpen) Close();
        }
    }
}
