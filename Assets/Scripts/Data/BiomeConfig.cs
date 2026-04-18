using UnityEngine;

namespace Survain.Data
{
    /// <summary>
    /// Configuration d'un biome : ambiance visuelle, climat, ressources disponibles.
    /// Un asset .asset par biome dans Assets/ScriptableObjects/Biomes/.
    ///
    /// Squelette POC : on garde minimal. Les champs détaillés (tables de loot,
    /// densités d'arbres, courbes de température, sons d'ambiance, etc.)
    /// seront ajoutés au fil des sprints quand les systèmes consommateurs existeront.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BiomeConfig",
        menuName = "Survain/Data/Biome Config",
        order = 10)]
    public sealed class BiomeConfig : ScriptableObject
    {
        public enum BiomeType
        {
            ForetTemperee = 0,
            Plaine = 1,
            Toundra = 2,
            DesertAride = 3,
        }

        [Header("Identité")]
        [Tooltip("Nom affiché dans l'UI et les logs.")]
        [SerializeField] private string displayName = "Forêt Tempérée";

        [Tooltip("Type énuméré du biome — utilisé pour les règles de gameplay.")]
        [SerializeField] private BiomeType biomeType = BiomeType.ForetTemperee;

        [Header("Ambiance")]
        [Tooltip("Couleur dominante du biome (ciel, brouillard, teinte générale).")]
        [SerializeField] private Color ambientColor = new Color(0.55f, 0.65f, 0.45f, 1f);

        [Header("Climat (placeholder POC)")]
        [Tooltip("Température moyenne en °C — purement indicatif au stade POC.")]
        [Range(-30f, 50f)]
        [SerializeField] private float averageTemperatureCelsius = 12f;

        // Accesseurs publics — lecture seule depuis l'extérieur.
        public string DisplayName => displayName;
        public BiomeType Type => biomeType;
        public Color AmbientColor => ambientColor;
        public float AverageTemperatureCelsius => averageTemperatureCelsius;
    }
}
