using UnityEngine;
using Survain.Core;
using Survain.Gameplay.Inventories;
using Survain.Items;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Outil équipé du joueur, dérivé du slot actif de la hotbar.
    ///
    /// Refondu au sprint #7 : ne porte plus sa propre liste d'outils. La hotbar (instance
    /// d'Inventory, capacité 4 par convention) est la source de vérité. PlayerEquipment
    /// pointe vers un slot index de cette hotbar et expose le ToolData qui s'y trouve
    /// (ou null si le slot est vide / ne contient pas un ToolData).
    ///
    /// L'API publique (CurrentTool, CurrentSlotIndex, SetTool, OnCurrentToolChanged) reste
    /// stable : PlayerHarvester n'a aucune raison d'être touché.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [Tooltip("Inventaire 'hotbar' du joueur. Chaque slot peut porter un ToolData équipable.")]
        [SerializeField] private Inventory _hotbar;

        [Tooltip("Index du slot équipé au démarrage. -1 = aucun.")]
        [SerializeField] private int _initialSlotIndex = 0;

        public ToolData CurrentTool { get; private set; }
        public int CurrentSlotIndex { get; private set; } = -1;

        /// <summary>Émis quand l'outil courant change. Signature : (previous, current).</summary>
        public event System.Action<ToolData, ToolData> OnCurrentToolChanged;

        private void Awake()
        {
            if (_hotbar == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerEquipment : _hotbar non assigné.", this);
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            if (_hotbar != null) _hotbar.OnSlotChanged += OnHotbarSlotChanged;
        }

        private void OnDisable()
        {
            if (_hotbar != null) _hotbar.OnSlotChanged -= OnHotbarSlotChanged;
        }

        private void Start()
        {
            // Sélection initiale tentée au Start (après que tous les Awake aient peuplé la hotbar).
            if (_initialSlotIndex >= 0 && _initialSlotIndex < _hotbar.Capacity)
            {
                SetTool(_initialSlotIndex);
            }
        }

        /// <summary>
        /// Sélectionne un slot de la hotbar. L'outil exposé devient le ToolData du slot
        /// (ou null si vide / item non-Tool). Index hors plage = unequip.
        /// </summary>
        public void SetTool(int slotIndex)
        {
            ToolData target = null;
            if (_hotbar != null && slotIndex >= 0 && slotIndex < _hotbar.Capacity)
            {
                target = _hotbar.Get(slotIndex).Item as ToolData;
                CurrentSlotIndex = slotIndex;
            }
            else
            {
                CurrentSlotIndex = -1;
            }

            if (target == CurrentTool) return;

            var previous = CurrentTool;
            CurrentTool = target;

            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Outil équipé : '{(target != null ? target.Id : "aucun")}' (slot {CurrentSlotIndex}).",
                this);

            OnCurrentToolChanged?.Invoke(previous, CurrentTool);
        }

        /// <summary>Nombre de slots de la hotbar (proxy direct).</summary>
        public int SlotCount => _hotbar != null ? _hotbar.Capacity : 0;

        // ─── Réaction aux changements de la hotbar ──────────────────────────

        private void OnHotbarSlotChanged(int index, InventorySlot before, InventorySlot after)
        {
            if (index != CurrentSlotIndex) return;

            // L'item du slot équipé a changé → reéquiper le bon ToolData.
            var newTool = after.Item as ToolData;
            if (newTool == CurrentTool) return;

            var previous = CurrentTool;
            CurrentTool = newTool;

            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Outil équipé mis à jour suite à changement de slot : '{(newTool != null ? newTool.Id : "aucun")}'.",
                this);

            OnCurrentToolChanged?.Invoke(previous, CurrentTool);
        }
    }
}
