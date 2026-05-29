using UnityEngine;
using Survain.Items;

namespace Survain.Gameplay.Inventories
{
    /// <summary>
    /// Helper statique pour spawner un WorldItem au monde.
    /// Factorise la logique partagée entre ResourceNode.SpawnDrop et le drop manuel
    /// depuis l'inventaire (drag hors UI), introduit en phase 3 de l'issue #7.
    /// </summary>
    public static class WorldItemSpawner
    {
        /// <summary>
        /// Crée un GameObject portant un WorldItem configuré à la position donnée.
        /// Retourne le composant WorldItem créé.
        /// </summary>
        public static WorldItem Spawn(ItemData item, int quantity, Vector3 position)
        {
            var go = new GameObject($"WorldItem_{(item != null ? item.Id : "unknown")}");
            go.transform.position = position;
            var worldItem = go.AddComponent<WorldItem>();
            worldItem.Configure(item, quantity);
            return worldItem;
        }
    }
}
