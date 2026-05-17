using UnityEngine;

namespace Survain.Items
{
    /// <summary>
    /// Arme de mêlée ou distance. Squelette POC — sera enrichi au Sprint Combat
    /// (types de dégâts, vitesses d'attaque, portées, animations associées).
    /// </summary>
    [CreateAssetMenu(
        fileName = "WeaponData",
        menuName = "Survain/Items/Weapon",
        order = 52)]
    public sealed class WeaponData : ItemData
    {
        [Header("Combat")]
        [Tooltip("Dégâts de base par coup.")]
        [Min(0)]
        [SerializeField] private int _damage = 1;

        [Tooltip("Portée effective en mètres.")]
        [Range(0.5f, 30f)]
        [SerializeField] private float _range = 1.5f;

        [Tooltip("Durabilité max (nombre de coups avant casse). 0 = incassable.")]
        [Min(0)]
        [SerializeField] private int _maxDurability = 100;

        public int Damage => _damage;
        public float Range => _range;
        public int MaxDurability => _maxDurability;

        public override ItemType Type => ItemType.Weapon;
    }
}
