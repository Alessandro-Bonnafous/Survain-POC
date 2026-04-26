using UnityEngine;

namespace Survain.Data
{
    /// <summary>
    /// Paramètres de génération procédurale d'un terrain.
    /// Conteneur de données pur — aucune logique ici.
    /// Consommé par TerrainGenerator (Survain.Gameplay.World).
    /// </summary>
    [CreateAssetMenu(
        fileName = "TerrainGenerationSettings",
        menuName = "Survain/Data/Terrain Generation Settings",
        order = 20)]
    public sealed class TerrainGenerationSettings : ScriptableObject
    {
        [Header("Dimensions")]
        [Tooltip("Taille du terrain en mètres (carré).")]
        [Range(50f, 500f)]
        [SerializeField] private float worldSize = 100f;

        [Tooltip("Subdivisions par côté. N subdivisions → 6×N² vertices (vertices dupliqués par triangle pour flat shading).")]
        [Range(16, 128)]
        [SerializeField] private int subdivisions = 80;

        [Header("Relief (Perlin multi-octaves)")]
        [Tooltip("Amplitude maximale du relief (mètres).")]
        [Range(1f, 50f)]
        [SerializeField] private float heightAmplitude = 12f;

        [Tooltip("Fréquence de base du Perlin. Plus élevé = collines plus petites.")]
        [Range(0.001f, 0.1f)]
        [SerializeField] private float baseFrequency = 0.015f;

        [Tooltip("Nombre d'octaves empilées.")]
        [Range(1, 6)]
        [SerializeField] private int octaves = 4;

        [Tooltip("Atténuation d'amplitude par octave (typiquement 0.5).")]
        [Range(0.1f, 0.9f)]
        [SerializeField] private float persistence = 0.5f;

        [Tooltip("Multiplication de fréquence par octave (typiquement 2).")]
        [Range(1.5f, 4f)]
        [SerializeField] private float lacunarity = 2f;

        [Header("Coloration par altitude")]
        [Tooltip("Gradient appliqué en vertex color selon l'altitude normalisée [0..1].")]
        [SerializeField] private Gradient altitudeGradient = CreateDefaultGradient();

        [Header("Placeholders")]
        [Tooltip("Densité cible (nombre de placeholders par 100 m²).")]
        [Range(0, 50)]
        [SerializeField] private int placeholderDensityPer100SqM = 8;

        [Tooltip("Pente maximale (degrés) sur laquelle un placeholder peut apparaître.")]
        [Range(0f, 60f)]
        [SerializeField] private float maxSlopeDegrees = 25f;

        [Tooltip("Ratio d'arbres dans la distribution. Le reste sera des rochers.")]
        [Range(0f, 1f)]
        [SerializeField] private float treeRatio = 0.7f;

        // Accesseurs
        public float WorldSize => worldSize;
        public int Subdivisions => subdivisions;
        public float HeightAmplitude => heightAmplitude;
        public float BaseFrequency => baseFrequency;
        public int Octaves => octaves;
        public float Persistence => persistence;
        public float Lacunarity => lacunarity;
        public Gradient AltitudeGradient => altitudeGradient;
        public int PlaceholderDensityPer100SqM => placeholderDensityPer100SqM;
        public float MaxSlopeDegrees => maxSlopeDegrees;
        public float TreeRatio => treeRatio;

        private static Gradient CreateDefaultGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.25f, 0.45f, 0.20f), 0.00f), // vert foncé
                    new GradientColorKey(new Color(0.45f, 0.60f, 0.25f), 0.35f), // vert moyen
                    new GradientColorKey(new Color(0.60f, 0.50f, 0.35f), 0.60f), // terre
                    new GradientColorKey(new Color(0.55f, 0.55f, 0.55f), 0.80f), // roche
                    new GradientColorKey(new Color(0.95f, 0.95f, 0.98f), 1.00f), // neige
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });
            return g;
        }
    }
}
