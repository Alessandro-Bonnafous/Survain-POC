using UnityEngine;

namespace Survain.Items
{
    /// <summary>
    /// Ressource brute ou raffinée (bois, pierre, fibre, minerai...). Matière première
    /// consommée par les recettes de craft.
    ///
    /// Par défaut stackable jusqu'à 99 — adaptable par asset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ResourceData",
        menuName = "Survain/Items/Resource",
        order = 50)]
    public sealed class ResourceData : ItemData
    {
        public override ItemType Type => ItemType.Resource;
    }
}
