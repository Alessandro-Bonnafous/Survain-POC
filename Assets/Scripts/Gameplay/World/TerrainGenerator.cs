using UnityEngine;
using UnityEngine.Rendering;
using Survain.Core;
using Survain.Data;

namespace Survain.Gameplay.World
{
    /// <summary>
    /// Génère un terrain procédural low-poly flat shaded + des placeholders (arbres/rochers).
    /// Déterministe à partir d'un seed. Placé à la racine (0,0,0) par convention, terrain centré.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshCollider))]
    public sealed class TerrainGenerator : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private TerrainGenerationSettings settings;

        [Tooltip("Source du seed. Si null, seedOverride est utilisé directement.")]
        [SerializeField] private GameSettings gameSettings;

        [Tooltip("Override du seed. 0 = utilise GameSettings.WorldSeed (ou aléatoire si GameSettings.WorldSeed vaut aussi 0).")]
        [SerializeField] private int seedOverride = 0;

        [Header("Matériaux")]
        [SerializeField] private Material terrainMaterial;
        [SerializeField] private Material treeMaterial;
        [SerializeField] private Material rockMaterial;

        [Header("Auto")]
        [Tooltip("Génère automatiquement au Start().")]
        [SerializeField] private bool generateOnStart = true;

        // État runtime
        private System.Random seededRandom;
        private Vector2 noiseOffset;
        private int currentSeed;
        private Transform placeholdersRoot;

        private const string PlaceholdersRootName = "Placeholders";

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            if (generateOnStart) Generate();
        }

        // ─── API publique ───────────────────────────────────────────────────

        [ContextMenu("Generate")]
        public void Generate()
        {
            if (settings == null)
            {
                SurvainLog.Error(SurvainLog.Category.World,
                    "TerrainGenerator : settings non assigné.", this);
                return;
            }

            int seed = seedOverride != 0
                ? seedOverride
                : (gameSettings != null ? gameSettings.WorldSeed : 0);

            InitRandom(seed);

            SurvainLog.Info(SurvainLog.Category.World,
                $"Génération du terrain (seed={currentSeed}, taille={settings.WorldSize}m, subdivs={settings.Subdivisions}).",
                this);

            var mesh = BuildMesh();
            ApplyMesh(mesh);

            ClearPlaceholders();
            SpawnPlaceholders();

            SurvainLog.Info(SurvainLog.Category.World, "Terrain généré.", this);
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf.sharedMesh != null)
            {
                DestroyMeshSafely(mf.sharedMesh);
                mf.sharedMesh = null;
            }
            GetComponent<MeshCollider>().sharedMesh = null;
            ClearPlaceholders();
        }

        // ─── Random / bruit ─────────────────────────────────────────────────

        private void InitRandom(int seed)
        {
            if (seed == 0)
            {
                seed = UnityEngine.Random.Range(int.MinValue + 1, int.MaxValue);
                SurvainLog.Info(SurvainLog.Category.World,
                    $"Seed=0 → seed aléatoire généré : {seed}", this);
            }
            currentSeed = seed;
            seededRandom = new System.Random(seed);
            noiseOffset = new Vector2(
                (float)(seededRandom.NextDouble() * 10000.0 - 5000.0),
                (float)(seededRandom.NextDouble() * 10000.0 - 5000.0));
        }

        private float SampleHeight(float worldX, float worldZ)
        {
            float amplitude = 1f;
            float frequency = settings.BaseFrequency;
            float noiseHeight = 0f;
            float maxAmplitude = 0f;

            for (int i = 0; i < settings.Octaves; i++)
            {
                float sampleX = (worldX + noiseOffset.x) * frequency;
                float sampleZ = (worldZ + noiseOffset.y) * frequency;
                float perlin = Mathf.PerlinNoise(sampleX, sampleZ) * 2f - 1f; // [-1..1]
                noiseHeight += perlin * amplitude;
                maxAmplitude += amplitude;
                amplitude *= settings.Persistence;
                frequency *= settings.Lacunarity;
            }

            noiseHeight /= maxAmplitude;          // [-1..1]
            noiseHeight = (noiseHeight + 1f) * 0.5f; // [0..1]
            return noiseHeight * settings.HeightAmplitude;
        }

        // ─── Mesh ───────────────────────────────────────────────────────────

        private Mesh BuildMesh()
        {
            int subs = settings.Subdivisions;
            float size = settings.WorldSize;
            float cellSize = size / subs;
            float halfSize = size * 0.5f;

            int quadCount = subs * subs;
            int triCount = quadCount * 2;
            int vertCount = triCount * 3;

            var vertices = new Vector3[vertCount];
            var colors = new Color[vertCount];
            var triangles = new int[triCount * 3];

            int vi = 0;
            for (int z = 0; z < subs; z++)
            {
                for (int x = 0; x < subs; x++)
                {
                    float x0 = -halfSize + x * cellSize;
                    float x1 = x0 + cellSize;
                    float z0 = -halfSize + z * cellSize;
                    float z1 = z0 + cellSize;

                    var v00 = new Vector3(x0, SampleHeight(x0, z0), z0);
                    var v10 = new Vector3(x1, SampleHeight(x1, z0), z0);
                    var v01 = new Vector3(x0, SampleHeight(x0, z1), z1);
                    var v11 = new Vector3(x1, SampleHeight(x1, z1), z1);

                    // Triangle 1 : v00, v01, v11 (CCW vu du dessus)
                    vertices[vi + 0] = v00;
                    vertices[vi + 1] = v01;
                    vertices[vi + 2] = v11;
                    triangles[vi + 0] = vi + 0;
                    triangles[vi + 1] = vi + 1;
                    triangles[vi + 2] = vi + 2;
                    vi += 3;

                    // Triangle 2 : v00, v11, v10
                    vertices[vi + 0] = v00;
                    vertices[vi + 1] = v11;
                    vertices[vi + 2] = v10;
                    triangles[vi + 0] = vi + 0;
                    triangles[vi + 1] = vi + 1;
                    triangles[vi + 2] = vi + 2;
                    vi += 3;
                }
            }

            // Coloration par altitude normalisée sur le min/max réel du mesh
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 0; i < vertCount; i++)
            {
                if (vertices[i].y < minY) minY = vertices[i].y;
                if (vertices[i].y > maxY) maxY = vertices[i].y;
            }
            float range = Mathf.Max(0.0001f, maxY - minY);
            for (int i = 0; i < vertCount; i++)
            {
                float t = (vertices[i].y - minY) / range;
                colors[i] = settings.AltitudeGradient.Evaluate(t);
            }

            var mesh = new Mesh
            {
                name = "ProceduralTerrain",
                indexFormat = IndexFormat.UInt32,
            };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            mesh.RecalculateNormals(); // vertices dupliqués par face → normales par face → flat shading
            mesh.RecalculateBounds();
            return mesh;
        }

        private void ApplyMesh(Mesh mesh)
        {
            var mf = GetComponent<MeshFilter>();
            var mr = GetComponent<MeshRenderer>();
            var mc = GetComponent<MeshCollider>();

            // Libérer l'ancien mesh pour éviter la fuite mémoire
            if (mf.sharedMesh != null) DestroyMeshSafely(mf.sharedMesh);

            mf.sharedMesh = mesh;
            mc.sharedMesh = mesh;
            if (terrainMaterial != null) mr.sharedMaterial = terrainMaterial;
        }

        // ─── Placeholders ───────────────────────────────────────────────────

        private void ClearPlaceholders()
        {
            var existing = transform.Find(PlaceholdersRootName);
            if (existing != null)
            {
                if (Application.isPlaying) Destroy(existing.gameObject);
                else DestroyImmediate(existing.gameObject);
            }
            var go = new GameObject(PlaceholdersRootName);
            go.transform.SetParent(transform, false);
            placeholdersRoot = go.transform;
        }

        private void SpawnPlaceholders()
        {
            float size = settings.WorldSize;
            float area = size * size;
            int targetCount = Mathf.RoundToInt(area / 100f * settings.PlaceholderDensityPer100SqM);
            float halfSize = size * 0.5f;

            int placed = 0, attempts = 0, maxAttempts = targetCount * 10;
            var mc = GetComponent<MeshCollider>();

            while (placed < targetCount && attempts < maxAttempts)
            {
                attempts++;
                float x = ((float)seededRandom.NextDouble() * 2f - 1f) * halfSize;
                float z = ((float)seededRandom.NextDouble() * 2f - 1f) * halfSize;

                // Raycast sur le MeshCollider uniquement (on vise précisément notre terrain)
                var rayOrigin = new Vector3(x, 10000f, z);
                if (!mc.Raycast(new Ray(rayOrigin, Vector3.down), out RaycastHit hit, 20000f))
                    continue;

                float slope = Vector3.Angle(hit.normal, Vector3.up);
                if (slope > settings.MaxSlopeDegrees) continue;

                bool isTree = seededRandom.NextDouble() < settings.TreeRatio;
                SpawnPlaceholder(hit.point, isTree);
                placed++;
            }

            SurvainLog.Info(SurvainLog.Category.World,
                $"Placeholders générés : {placed}/{targetCount} (tentatives : {attempts}).", this);
        }

        private void SpawnPlaceholder(Vector3 groundPos, bool isTree)
        {
            var go = GameObject.CreatePrimitive(isTree ? PrimitiveType.Cube : PrimitiveType.Sphere);
            go.name = isTree ? "PH_Tree" : "PH_Rock";
            go.transform.SetParent(placeholdersRoot, false);

            // Pas de collider physique sur les placeholders au POC
            var c = go.GetComponent<Collider>();
            if (c != null)
            {
                if (Application.isPlaying) Destroy(c);
                else DestroyImmediate(c);
            }

            var rend = go.GetComponent<Renderer>();

            if (isTree)
            {
                go.transform.localScale = new Vector3(0.8f, 3.0f, 0.8f);
                go.transform.localPosition = groundPos + Vector3.up * 1.5f;
                if (treeMaterial != null) rend.sharedMaterial = treeMaterial;
            }
            else
            {
                float s = 0.5f + (float)seededRandom.NextDouble() * 0.9f;
                go.transform.localScale = Vector3.one * s;
                go.transform.localPosition = groundPos + Vector3.up * s * 0.4f;
                if (rockMaterial != null) rend.sharedMaterial = rockMaterial;
            }
        }

        // ─── Utilitaires ────────────────────────────────────────────────────

        private static void DestroyMeshSafely(Mesh m)
        {
            if (Application.isPlaying) Destroy(m);
            else DestroyImmediate(m);
        }
    }
}
