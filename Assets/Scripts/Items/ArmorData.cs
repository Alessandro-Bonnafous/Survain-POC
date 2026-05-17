using UnityEngine;

namespace Survain.Items
{
    /// <summary>
    /// Pièce d'armure. Squelette POC — sera enrichi quand le système de combat et
    /// d'équipement arrivera (slot, résistances par type, set bonuses).
    /// </summary>
    [CreateAssetMenu(
        fileName = "ArmorData",
        menuName = "Survain/Items/Armor",
        order = 53)]
    public sealed class ArmorData : ItemData
    {
        [Header("Protection")]
        [Tooltip("Réduction de dégâts plate (placeholder POC — sera remplacé par un modèle plus riche).")]
        [Min(0)]
        [SerializeField] private int _defense = 1;

        [Tooltip("Durabilité max (points de dégâts encaissés avant casse). 0 = incassable.")]
        [Min(0)]
        [SerializeField] private int _maxDurability = 100;

        public int Defense => _defense;
        public int MaxDurability => _maxDurability;

        public override ItemType Type => ItemType.Armor;
    }
}
