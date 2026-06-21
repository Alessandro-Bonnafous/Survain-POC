using UnityEngine;

namespace Survain.Gameplay.Combat
{
    /// <summary>
    /// Coup typé (combat #16, Phase B / B4) : décompose un total de dégâts en une part de
    /// <b>biome</b> et une part <b>physique</b> (spec : 80 % / 20 %, Q2). Struct immuable, sans
    /// allocation — construite via <see cref="Split"/> à partir d'un total + d'une fraction de biome.
    ///
    /// <para>En B4 (sans armures #85), le consommateur applique simplement <see cref="Total"/> ; la
    /// décomposition (<see cref="BiomeAmount"/> / <see cref="PhysicalAmount"/> / <see cref="BiomeType"/>)
    /// est le crochet pour les résistances typées d'armure (B5 : on atténuera chaque part selon son type).
    /// Les chiffres restent des placeholders ajustables (équilibrage #88).</para>
    /// </summary>
    public readonly struct DamageInfo
    {
        /// <summary>Type de la part de biome de ce coup (jamais <see cref="DamageType.Physical"/>).</summary>
        public readonly DamageType BiomeType;

        /// <summary>Part de dégâts du biome (≥ 0).</summary>
        public readonly float BiomeAmount;

        /// <summary>Part de dégâts physiques (≥ 0).</summary>
        public readonly float PhysicalAmount;

        /// <summary>Total infligé = biome + physique (ce qu'on applique aux PV tant qu'il n'y a pas de résistances).</summary>
        public float Total => BiomeAmount + PhysicalAmount;

        public DamageInfo(DamageType biomeType, float biomeAmount, float physicalAmount)
        {
            BiomeType = biomeType;
            BiomeAmount = Mathf.Max(0f, biomeAmount);
            PhysicalAmount = Mathf.Max(0f, physicalAmount);
        }

        /// <summary>
        /// Construit un coup typé à partir d'un <paramref name="total"/> et d'une
        /// <paramref name="biomeFraction"/> (part de biome dans [0..1] ; le reste est physique).
        /// Spec : <c>biomeFraction = 0.8</c>. Placeholder ajustable (Q2 / équilibrage #88).
        /// </summary>
        public static DamageInfo Split(float total, float biomeFraction, DamageType biomeType)
        {
            total = Mathf.Max(0f, total);
            float f = Mathf.Clamp01(biomeFraction);
            float biome = total * f;
            return new DamageInfo(biomeType, biome, total - biome);
        }

        /// <summary>Total arrondi à l'entier le plus proche — pour appliquer aux PV entiers (POC).</summary>
        public int TotalRounded => Mathf.RoundToInt(Total);

        public override string ToString()
            => $"{Total:0.#} dmg [{BiomeType} {BiomeAmount:0.#} + Physique {PhysicalAmount:0.#}]";
    }
}
