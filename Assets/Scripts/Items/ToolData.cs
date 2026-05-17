using UnityEngine;

namespace Survain.Items
{
    /// <summary>
    /// Outil de récolte (hache, pioche, faucille...). Caractérisé par son ToolType,
    /// utilisé par les ResourceNodeData pour décider si la récolte est possible.
    ///
    /// HarvestSpeed multiplie la vitesse de récolte de base du nœud
    /// (ResourceNodeData.HarvestSeconds) : 1.0 = vitesse nominale, 2.0 = deux fois plus rapide.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ToolData",
        menuName = "Survain/Items/Tool",
        order = 51)]
    public sealed class ToolData : ItemData
    {
        [Header("Outil")]
        [Tooltip("Famille d'outil — détermine quels nœuds de ressources sont récoltables.")]
        [SerializeField] private ToolType _toolType = ToolType.None;

        [Tooltip("Multiplicateur de vitesse de récolte (1.0 = vitesse nominale du nœud).")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _harvestSpeed = 1f;

        [Tooltip("Durabilité max (nombre d'utilisations avant casse). 0 = incassable.")]
        [Min(0)]
        [SerializeField] private int _maxDurability = 100;

        public ToolType ToolType => _toolType;
        public float HarvestSpeed => _harvestSpeed;
        public int MaxDurability => _maxDurability;

        public override ItemType Type => ItemType.Tool;
    }
}
