using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("worldSize")]
        [SerializeField] private float _worldSize = 100f;

        [Tooltip("Subdivisions par côté. N subdivisions → 6×N² vertices (vertices dupliqués par triangle pour flat shading).")]
        [Range(16, 128)]
        [FormerlySerializedAs("subdivisions")]
        [SerializeField] private int _subdivisions = 80;

        [Header("Relief (Perlin multi-octaves)")]
        [Tooltip("Amplitude maximale du relief (mètres).")]
        [Range(1f, 50f)]
        [FormerlySerializedAs("heightAmplitude")]
        [SerializeField] private float _heightAmplitude = 12f;

        [Tooltip("Fréquence de base du Perlin. Plus élevé = collines plus petites.")]
        [Range(0.001f, 0.1f)]
        [FormerlySerializedAs("baseFrequency")]
        [SerializeField] private float _baseFrequency = 0.015f;

        [Tooltip("Nombre d'octaves empilées.")]
        [Range(1, 6)]
        [FormerlySerializedAs("octaves")]
        [SerializeField] private int _octaves = 4;

        [Tooltip("Atténuation d'amplitude par octave (typiquement 0.5).")]
        [Range(0.1f, 0.9f)]
        [FormerlySerializedAs("persistence")]
        [SerializeField] private float _persistence = 0.5f;

        [Tooltip("Multiplication de fréquence par octave (typiquement 2).")]
        [Range(1.5f, 4f)]
        [FormerlySerializedAs("lacunarity")]
        [SerializeField] private float _lacunarity = 2f;

        [Header("Coloration par altitude")]
        [Tooltip("Gradient appliqué en vertex color selon l'altitude normalisée [0..1].")]
        [FormerlySerializedAs("altitudeGradient")]
        [SerializeField] private Gradient _altitudeGradient = CreateDefaultGradient();

        [Header("Bordure (falloff)")]
        [Tooltip("Aplanit le relief vers les bords jusqu'à une altitude commune → jointure de zones " +
                 "adjacentes franchissable (#18). Désactivé = comportement historique.")]
        [SerializeField] private bool _edgeFalloff = false;

        [Tooltip("Altitude (m) vers laquelle le terrain converge au bord. À PARTAGER entre terrains " +
                 "adjacents pour que la jointure soit plane et traversable.")]
        [SerializeField] private float _edgeFalloffHeight = 0f;

        [Tooltip("Largeur (m) de la transition depuis le bord vers l'intérieur.")]
        [Min(0.1f)]
        [SerializeField] private float _edgeFalloffWidth = 8f;

        // Accesseurs
        public float WorldSize => _worldSize;
        public int Subdivisions => _subdivisions;
        public float HeightAmplitude => _heightAmplitude;
        public float BaseFrequency => _baseFrequency;
        public int Octaves => _octaves;
        public float Persistence => _persistence;
        public float Lacunarity => _lacunarity;
        public Gradient AltitudeGradient => _altitudeGradient;
        public bool EdgeFalloff => _edgeFalloff;
        public float EdgeFalloffHeight => _edgeFalloffHeight;
        public float EdgeFalloffWidth => _edgeFalloffWidth;

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
