using UnityEngine;
using Survain.Core;
using Survain.Gameplay.Inventories;
using Survain.Gameplay.Player;
using Survain.Items;

namespace Survain.UI
{
    /// <summary>
    /// Pilote la barre du bas qui affiche les slots de la hotbar. Délègue chaque slot à
    /// un InventorySlotView (le rendu reste local au slot) ; cette classe gère uniquement
    /// le binding initial et la matérialisation visuelle du slot équipé courant.
    ///
    /// Conventions :
    ///  - _slotViews.Length doit correspondre à _hotbar.Capacity (vérifié au Start).
    ///  - Les vues sont ordonnées slot-by-slot (index 0 du tableau = slot 0 de la hotbar).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HotbarUI : MonoBehaviour
    {
        [Tooltip("Inventaire 'hotbar' à afficher.")]
        [SerializeField] private Inventory _hotbar;

        [Tooltip("Équipement joueur, source du slot courant pour la mise en évidence.")]
        [SerializeField] private PlayerEquipment _equipment;

        [Tooltip("Vues de slots, dans l'ordre. Doit avoir exactement _hotbar.Capacity éléments.")]
        [SerializeField] private InventorySlotView[] _slotViews;

        private void Awake()
        {
            if (_hotbar == null || _equipment == null || _slotViews == null)
            {
                SurvainLog.Error(SurvainLog.Category.UI,
                    "HotbarUI : _hotbar, _equipment ou _slotViews non assignés.", this);
                enabled = false;
                return;
            }

            if (_slotViews.Length != _hotbar.Capacity)
            {
                SurvainLog.Error(SurvainLog.Category.UI,
                    $"HotbarUI : nombre de vues ({_slotViews.Length}) ≠ capacité hotbar ({_hotbar.Capacity}).", this);
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            if (_equipment != null) _equipment.OnCurrentToolChanged += OnEquippedToolChanged;
        }

        private void OnDisable()
        {
            if (_equipment != null) _equipment.OnCurrentToolChanged -= OnEquippedToolChanged;
        }

        private void Start()
        {
            for (int i = 0; i < _slotViews.Length; i++)
            {
                if (_slotViews[i] == null) continue;
                _slotViews[i].Bind(_hotbar, i);
            }
            RefreshSelection();
        }

        private void OnEquippedToolChanged(ToolData previous, ToolData current)
        {
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            int currentIndex = _equipment != null ? _equipment.CurrentSlotIndex : -1;
            for (int i = 0; i < _slotViews.Length; i++)
            {
                if (_slotViews[i] == null) continue;
                _slotViews[i].SetSelected(i == currentIndex);
            }
        }
    }
}
