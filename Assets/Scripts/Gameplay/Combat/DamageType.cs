namespace Survain.Gameplay.Combat
{
    /// <summary>
    /// Type d'une part de dégât (combat #16, Phase B / B4). Un coup se décompose en une part
    /// <see cref="Physical"/> et une part de <b>biome</b> (spec : 80 % biome / 20 % physique, Q2).
    ///
    /// <para><b>Enum dédié, et non réemploi de <c>BiomeConfig.BiomeType</c></b> : (1) le combat a besoin
    /// d'un membre <see cref="Physical"/> qui n'a aucun sens côté génération de monde ; (2) la spec combat
    /// parle de biomes (« Vent », « Froid », « Côte maritime », « Montagne »…) qui ne mappent pas 1:1 sur
    /// le roster worldgen (ForetTemperee/Plaine/Toundra/DesertAride) — le roster d'éléments de combat est
    /// un choix de game design encore ouvert (gate Pascal / équilibrage #88). Découpler évite un couplage
    /// load-bearing entre worldgen et équilibrage combat. Les membres de biome reprennent pour l'instant
    /// le roster worldgen comme socle concret ; les renommer (Vent/Froid…) reste un ajustement #88.</para>
    /// </summary>
    public enum DamageType
    {
        /// <summary>Dégât physique brut (toujours présent, ~20 % du total selon la spec).</summary>
        Physical = 0,

        // ─── Parts de biome (placeholder roster, calqué sur BiomeConfig.BiomeType) ───
        ForetTemperee = 1,
        Plaine = 2,
        Toundra = 3,
        DesertAride = 4,
    }
}
