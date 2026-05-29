using System;
using UnityEngine;
using Survain.Core;
using Survain.Items;

namespace Survain.Gameplay.Inventories
{
    /// <summary>
    /// État d'un slot d'inventaire : item référencé + quantité. Slot vide = (null, 0).
    /// Struct readonly pour pouvoir comparer before/after par valeur dans Inventory.OnSlotChanged.
    ///
    /// Placé top-level dans le namespace (et non nested dans Inventory) pour éviter
    /// l'ambiguïté C# entre le type Inventory et le namespace Survain.Gameplay.Inventories
    /// quand on écrit `Inventory.Slot` depuis un autre fichier.
    /// </summary>
    public readonly struct InventorySlot
    {
        public readonly ItemData Item;
        public readonly int Quantity;

        public InventorySlot(ItemData item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }

        public bool IsEmpty => Item == null || Quantity <= 0;
        public static InventorySlot Empty => new InventorySlot(null, 0);
    }

    /// <summary>
    /// Conteneur d'items à capacité fixe. Sert de sac à dos (24) et de hotbar (4) sur le _Player.
    ///
    /// Convention : chaque instance vit sur un GameObject enfant dédié (_Player/Backpack/,
    /// _Player/Hotbar/) pour pouvoir en avoir plusieurs sans conflit GetComponent.
    ///
    /// API impérative côté écriture (TryAdd/TryRemove/Swap), événements côté lecture
    /// (OnSlotChanged + OnInventoryChanged). Aucune logique métier d'item — juste du
    /// stockage paramétré par MaxStackSize lu sur ItemData.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Inventory : MonoBehaviour
    {
        [Tooltip("Nombre de slots du conteneur. Backpack=24, Hotbar=4 typiquement.")]
        [Min(1)]
        [SerializeField] private int _capacity = 24;

        private InventorySlot[] _slots;

        /// <summary>Émis dès qu'un slot change (Add/Remove/Swap). Signature : (index, before, after).</summary>
        public event Action<int, InventorySlot, InventorySlot> OnSlotChanged;

        /// <summary>Émis après chaque opération qui a modifié au moins un slot (synthèse).</summary>
        public event Action OnInventoryChanged;

        public int Capacity => _capacity;

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            _slots = new InventorySlot[_capacity];
            for (int i = 0; i < _capacity; i++) _slots[i] = InventorySlot.Empty;
        }

        // ─── Lecture ────────────────────────────────────────────────────────

        public InventorySlot Get(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _capacity) return InventorySlot.Empty;
            return _slots[slotIndex];
        }

        /// <summary>Nombre total d'unités d'un item à travers tous les slots.</summary>
        public int Count(ItemData item)
        {
            if (item == null) return 0;
            int total = 0;
            for (int i = 0; i < _capacity; i++)
            {
                if (_slots[i].Item == item) total += _slots[i].Quantity;
            }
            return total;
        }

        public bool Contains(ItemData item) => Count(item) > 0;

        // ─── Écriture ───────────────────────────────────────────────────────

        /// <summary>
        /// Tente d'ajouter `amount` unités de `item` à l'inventaire. Remplit d'abord les stacks
        /// existants, puis crée de nouveaux stacks dans les slots vides. Retourne la quantité
        /// qui n'a PAS pu être ajoutée (0 = tout est rentré).
        /// </summary>
        public int TryAdd(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return amount;

            int remaining = amount;
            int maxStack = item.MaxStackSize;

            // 1. Remplir les stacks existants
            if (item.IsStackable)
            {
                for (int i = 0; i < _capacity && remaining > 0; i++)
                {
                    var slot = _slots[i];
                    if (slot.Item != item || slot.Quantity >= maxStack) continue;

                    int canFit = maxStack - slot.Quantity;
                    int toAdd = Mathf.Min(canFit, remaining);
                    SetSlot(i, new InventorySlot(item, slot.Quantity + toAdd));
                    remaining -= toAdd;
                }
            }

            // 2. Créer de nouveaux stacks dans les slots vides
            for (int i = 0; i < _capacity && remaining > 0; i++)
            {
                if (!_slots[i].IsEmpty) continue;

                int toAdd = Mathf.Min(maxStack, remaining);
                SetSlot(i, new InventorySlot(item, toAdd));
                remaining -= toAdd;
            }

            if (remaining != amount) OnInventoryChanged?.Invoke();
            return remaining;
        }

        /// <summary>
        /// Retire jusqu'à `amount` unités de `item`. Vide les stacks dans l'ordre des slots.
        /// Retourne la quantité effectivement retirée.
        /// </summary>
        public int TryRemove(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return 0;

            int removed = 0;
            int remaining = amount;

            for (int i = 0; i < _capacity && remaining > 0; i++)
            {
                var slot = _slots[i];
                if (slot.Item != item) continue;

                int toRemove = Mathf.Min(slot.Quantity, remaining);
                int newQty = slot.Quantity - toRemove;
                SetSlot(i, newQty > 0 ? new InventorySlot(item, newQty) : InventorySlot.Empty);
                removed += toRemove;
                remaining -= toRemove;
            }

            if (removed > 0) OnInventoryChanged?.Invoke();
            return removed;
        }

        /// <summary>Échange le contenu de deux slots. Retourne false si indices invalides.</summary>
        public bool Swap(int a, int b)
        {
            if (a < 0 || a >= _capacity || b < 0 || b >= _capacity) return false;
            if (a == b) return true;

            var slotA = _slots[a];
            var slotB = _slots[b];
            SetSlot(a, slotB);
            SetSlot(b, slotA);
            OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Échange le contenu d'un slot de CET inventaire avec un slot d'un AUTRE inventaire.
        /// Utilisé par le drag & drop UI pour déplacer un item entre backpack et hotbar.
        ///
        /// Note : aucune validation de MaxStackSize côté destination — on suppose que les
        /// slots échangés ont été créés par TryAdd (qui respecte MaxStackSize) et sont donc
        /// valides par construction. Si other == this, délègue à Swap classique.
        /// </summary>
        public bool SwapAcross(int thisIndex, Inventory other, int otherIndex)
        {
            if (other == null) return false;
            if (other == this) return Swap(thisIndex, otherIndex);
            if (thisIndex < 0 || thisIndex >= _capacity) return false;
            if (otherIndex < 0 || otherIndex >= other._capacity) return false;

            var slotHere = _slots[thisIndex];
            var slotThere = other._slots[otherIndex];
            SetSlot(thisIndex, slotThere);
            other.SetSlot(otherIndex, slotHere);
            OnInventoryChanged?.Invoke();
            other.OnInventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Transfère le contenu d'un slot source vers la première place dispo dans `target`.
        /// Réutilise TryAdd côté cible pour gérer le stacking. Retourne true si au moins
        /// une unité a été déplacée.
        /// </summary>
        public bool Transfer(int sourceSlot, Inventory target)
        {
            if (target == null) return false;
            if (sourceSlot < 0 || sourceSlot >= _capacity) return false;

            var slot = _slots[sourceSlot];
            if (slot.IsEmpty) return false;

            int notAdded = target.TryAdd(slot.Item, slot.Quantity);
            int moved = slot.Quantity - notAdded;
            if (moved == 0) return false;

            int newQty = slot.Quantity - moved;
            SetSlot(sourceSlot, newQty > 0 ? new InventorySlot(slot.Item, newQty) : InventorySlot.Empty);
            OnInventoryChanged?.Invoke();
            return true;
        }

        // ─── Helpers internes ───────────────────────────────────────────────

        private void SetSlot(int index, InventorySlot newSlot)
        {
            var before = _slots[index];
            if (SlotEquals(before, newSlot)) return;
            _slots[index] = newSlot;
            OnSlotChanged?.Invoke(index, before, newSlot);
        }

        private static bool SlotEquals(InventorySlot a, InventorySlot b)
        {
            return a.Item == b.Item && a.Quantity == b.Quantity;
        }

        // ─── Debug (Inspector) ──────────────────────────────────────────────

        [ContextMenu("Log inventory state")]
        private void DebugLogState()
        {
            for (int i = 0; i < _capacity; i++)
            {
                var s = _slots[i];
                string line = s.IsEmpty ? "(vide)" : $"{s.Quantity}x {s.Item.Id}";
                SurvainLog.Info(SurvainLog.Category.Gameplay, $"Inventory[{i}] = {line}", this);
            }
        }
    }
}
