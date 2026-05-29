using System;
using UnityEngine;

namespace Survain.Items
{
    /// <summary>
    /// Coût en ressources d'une ligne de recette de construction : un item + une quantité.
    /// Une structure porte un tableau de BuildCost (ex : 4× bois + 2× pierre).
    ///
    /// Struct sérialisable top-level dans le namespace (et non nested dans BuildingData),
    /// même raison qu'InventorySlot : éviter l'ambiguïté type/namespace et permettre la
    /// réutilisation par d'autres systèmes (réparation #11, futur craft #8).
    /// </summary>
    [Serializable]
    public struct BuildCost
    {
        [Tooltip("Ressource consommée à la pose.")]
        [SerializeField] private ItemData _item;

        [Tooltip("Quantité consommée.")]
        [Min(1)]
        [SerializeField] private int _amount;

        public BuildCost(ItemData item, int amount)
        {
            _item = item;
            _amount = Mathf.Max(1, amount);
        }

        public ItemData Item => _item;
        public int Amount => _amount;
    }
}
