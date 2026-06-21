namespace Survain.Gameplay.Combat
{
    /// <summary>
    /// Type d'une part de dégât (combat #16, Phase B / B4). Un coup se décompose en une part
    /// <see cref="Physical"/> et une part de <b>biome</b> (spec : 80 % biome / 20 % physique, Q2).
    ///
    /// <para><b>Enum dédié, et non réemploi de <c>BiomeConfig.BiomeType</c></b> : (1) le combat a besoin
    /// d'un membre <see cref="Physical"/> qui n'a aucun sens côté génération de monde ; (2) le roster de
    /// biomes de combat (arbitré par le PO : Forêt / Plaines / Montagnes / Côte maritime) ne mappe pas 1:1
    /// sur le roster worldgen (ForetTemperee/Plaine/Toundra/DesertAride). Découpler évite un couplage
    /// load-bearing entre worldgen et équilibrage combat. Les valeurs de dégâts restent des placeholders
    /// ajustables (#88).</para>
    /// </summary>
    public enum DamageType
    {
        /// <summary>Dégât physique brut (toujours présent, ~20 % du total selon la spec).</summary>
        Physical = 0,

        // ─── Parts de biome (roster combat arbitré par le PO) ───
        Foret = 1,
        Plaines = 2,
        Montagnes = 3,
        CoteMaritime = 4,
    }
}
