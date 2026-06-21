using UnityEngine;
using Survain.Gameplay.Combat;

namespace Survain.Items
{
    /// <summary>
    /// Arme de mêlée ou distance. Squelette POC enrichi au Sprint Combat (#16) du <b>modèle de dégâts
    /// typés</b> (B4) : une arme porte un biome et un split biome/physique, et sait fabriquer un
    /// <see cref="DamageInfo"/> via <see cref="BuildHit"/>.
    ///
    /// <para><b>Crochet Phase B</b> : ces champs sont le futur foyer des dégâts du joueur. Tant que le
    /// craft #8 n'équipe pas de vraies <c>WeaponData</c> (les armes du POC sont des outils hache/pioche),
    /// c'est <c>PlayerEnemyStrike</c> qui porte les placeholders actifs. Quand des armes craftables
    /// seront équipées, la source de dégâts lira <see cref="BuildHit"/> ici au lieu de ses placeholders.</para>
    /// </summary>
    [CreateAssetMenu(
        fileName = "WeaponData",
        menuName = "Survain/Items/Weapon",
        order = 52)]
    public sealed class WeaponData : ItemData
    {
        [Header("Combat")]
        [Tooltip("Dégâts de base par coup (total ; réparti biome/physique selon le split ci-dessous).")]
        [Min(0)]
        [SerializeField] private int _damage = 1;

        [Tooltip("Portée effective en mètres.")]
        [Range(0.5f, 30f)]
        [SerializeField] private float _range = 1.5f;

        [Tooltip("Durabilité max (nombre de coups avant casse). 0 = incassable.")]
        [Min(0)]
        [SerializeField] private int _maxDurability = 100;

        [Header("Dégâts typés (#16 B4 — placeholders, équilibrage #88)")]
        [Tooltip("Biome dont l'arme inflige la part principale de dégâts.")]
        [SerializeField] private DamageType _biomeDamageType = DamageType.Foret;

        [Tooltip("Part de dégâts de biome dans le total (spec Q2 : 0.8 = 80 % biome / 20 % physique).")]
        [Range(0f, 1f)]
        [SerializeField] private float _biomeDamageFraction = 0.8f;

        public int Damage => _damage;
        public float Range => _range;
        public int MaxDurability => _maxDurability;
        public DamageType BiomeDamageType => _biomeDamageType;
        public float BiomeDamageFraction => _biomeDamageFraction;

        /// <summary>Fabrique le coup typé de cette arme (total réparti biome/physique selon le split).</summary>
        public DamageInfo BuildHit() => DamageInfo.Split(_damage, _biomeDamageFraction, _biomeDamageType);

        public override ItemType Type => ItemType.Weapon;
    }
}
