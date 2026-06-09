using System.Collections.Generic;
using UnityEngine;
using Survain.Core;
using Survain.Data;
using Survain.Items;

namespace Survain.Gameplay.Items
{
    /// <summary>
    /// Spawn procédural de nœuds de ressources sur le terrain. Consomme un MeshCollider
    /// (typiquement celui du TerrainGenerator) et un seed (GameSettings.WorldSeed ou
    /// override local) pour un placement déterministe.
    ///
    /// Pour chaque entrée (ResourceNodeData + densité), tire des positions XZ aléatoires,
    /// raycast vertical sur le terrain, rejette les pentes trop fortes, et instancie
    /// un GameObject porteur d'un ResourceNode.
    ///
    /// Pattern cohérent avec TerrainGenerator (décision 2026-04-19) : pas de mesh ni
    /// d'instance versionnée — tout est régénéré au Start() / via ContextMenu.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)] // Doit s'exécuter APRÈS TerrainGenerator qui peuple le MeshCollider.
    public sealed class ResourceNodeSpawner : MonoBehaviour
    {
        [System.Serializable]
        public struct SpawnEntry
        {
            [Tooltip("SO décrivant le type de nœud à spawner.")]
            public ResourceNodeData data;

            [Tooltip("Densité cible (nombre de nœuds de ce type par 100 m² de terrain).")]
            [Range(0f, 20f)]
            public float densityPer100SqM;
        }

        [Header("Source du terrain")]
        [Tooltip("MeshCollider du terrain sur lequel les nœuds seront posés (raycast vertical).")]
        [SerializeField] private MeshCollider _terrainCollider;

        [Tooltip("Taille du terrain en mètres (carré). Doit correspondre à TerrainGenerationSettings.WorldSize.")]
        [Range(50f, 500f)]
        [SerializeField] private float _worldSize = 100f;

        [Header("Source du seed")]
        [Tooltip("GameSettings pour récupérer WorldSeed. Si null, seedOverride est utilisé directement.")]
        [SerializeField] private GameSettings _gameSettings;

        [Tooltip("Override du seed. 0 = utilise GameSettings.WorldSeed.")]
        [SerializeField] private int _seedOverride = 0;

        [Tooltip("Décalage de seed appliqué à celui du terrain pour éviter d'aligner les nœuds sur le bruit Perlin.")]
        [SerializeField] private int _seedSalt = 7919;

        [Header("Contraintes de placement")]
        [Tooltip("Pente maximale (degrés) sur laquelle un nœud peut apparaître.")]
        [Range(0f, 60f)]
        [SerializeField] private float _maxSlopeDegrees = 25f;

        [Tooltip("Distance minimale entre deux nœuds (m). 0 = pas de filtre.")]
        [Range(0f, 10f)]
        [SerializeField] private float _minNodeSpacing = 1.5f;

        [Header("Spawn")]
        [Tooltip("Liste des nœuds à spawner avec leur densité respective.")]
        [SerializeField] private List<SpawnEntry> _entries = new List<SpawnEntry>();

        [Tooltip("Génère automatiquement au Start().")]
        [SerializeField] private bool _generateOnStart = true;

        private const string NodesRootName = "Nodes";
        private Transform _nodesRoot;

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Start()
        {
            if (_generateOnStart) Generate();
        }

        // ─── API publique ───────────────────────────────────────────────────

        /// <summary>Régénère les nœuds avec un seed imposé (instance zone sauvage, #74).</summary>
        public void GenerateWithSeed(int seed)
        {
            _seedOverride = seed;
            Generate();
        }

        [ContextMenu("Generate")]
        public void Generate()
        {
            if (_terrainCollider == null)
            {
                SurvainLog.Error(SurvainLog.Category.World,
                    "ResourceNodeSpawner : terrainCollider non assigné.", this);
                return;
            }

            // Garde-fou : raycaster sur un MeshCollider sans mesh échoue silencieusement.
            // Sans ce check on aurait 0 nœud placé sans le moindre indice.
            if (_terrainCollider.sharedMesh == null)
            {
                SurvainLog.Error(SurvainLog.Category.World,
                    "ResourceNodeSpawner : le MeshCollider n'a pas de sharedMesh. " +
                    "Si TerrainGenerator est dans la scène, vérifier que son DefaultExecutionOrder " +
                    "est bien inférieur à celui du spawner.", this);
                return;
            }

            int seed = ResolveSeed();
            var rng = new System.Random(seed);

            SurvainLog.Info(SurvainLog.Category.World,
                $"ResourceNodeSpawner : spawn (seed={seed}, taille={_worldSize}m).", this);

            ClearNodes();

            var placedPositions = new List<Vector3>(64);
            int totalPlaced = 0;

            foreach (var entry in _entries)
            {
                if (entry.data == null || entry.densityPer100SqM <= 0f) continue;
                totalPlaced += SpawnEntries(entry, rng, placedPositions);
            }

            SurvainLog.Info(SurvainLog.Category.World,
                $"ResourceNodeSpawner : {totalPlaced} nœuds placés.", this);
        }

        [ContextMenu("Clear")]
        public void Clear() => ClearNodes();

        // ─── Internals ──────────────────────────────────────────────────────

        private int ResolveSeed()
        {
            int baseSeed = _seedOverride != 0
                ? _seedOverride
                : (_gameSettings != null ? _gameSettings.WorldSeed : 0);

            if (baseSeed == 0)
            {
                // Tire un seed runtime cohérent avec le pattern du TerrainGenerator
                // (qui tire son propre seed aléatoire si WorldSeed = 0).
                baseSeed = Random.Range(int.MinValue + 1, int.MaxValue);
                SurvainLog.Info(SurvainLog.Category.World,
                    $"ResourceNodeSpawner : seed=0 → seed aléatoire {baseSeed}.", this);
            }

            return baseSeed ^ _seedSalt;
        }

        private int SpawnEntries(SpawnEntry entry, System.Random rng, List<Vector3> placedPositions)
        {
            float area = _worldSize * _worldSize;
            int target = Mathf.RoundToInt(area / 100f * entry.densityPer100SqM);
            float half = _worldSize * 0.5f;
            int maxAttempts = target * 10;
            int placed = 0;
            int attempts = 0;

            // Centre les tirages sur le terrain réel (bounds.center) et non sur l'origine du monde :
            // permet de poser des nœuds sur un terrain DÉCALÉ (ex. la zone sauvage adjacente, #18).
            // Pour un terrain centré à l'origine, center.x/z ≈ 0 → comportement inchangé.
            Vector3 center = _terrainCollider.bounds.center;

            while (placed < target && attempts < maxAttempts)
            {
                attempts++;

                float x = center.x + ((float)rng.NextDouble() * 2f - 1f) * half;
                float z = center.z + ((float)rng.NextDouble() * 2f - 1f) * half;

                var rayOrigin = new Vector3(x, 10000f, z);
                if (!_terrainCollider.Raycast(new Ray(rayOrigin, Vector3.down),
                        out RaycastHit hit, 20000f))
                    continue;

                float slope = Vector3.Angle(hit.normal, Vector3.up);
                if (slope > _maxSlopeDegrees) continue;

                if (_minNodeSpacing > 0f && IsTooClose(hit.point, placedPositions, _minNodeSpacing))
                    continue;

                SpawnNode(entry.data, hit.point);
                placedPositions.Add(hit.point);
                placed++;
            }

            SurvainLog.Info(SurvainLog.Category.World,
                $"ResourceNodeSpawner : '{entry.data.Id}' → {placed}/{target} placés " +
                $"(tentatives : {attempts}).", this);

            return placed;
        }

        private static bool IsTooClose(Vector3 pos, List<Vector3> existing, float minDist)
        {
            float minSqr = minDist * minDist;
            for (int i = 0; i < existing.Count; i++)
            {
                if ((existing[i] - pos).sqrMagnitude < minSqr) return true;
            }
            return false;
        }

        private void SpawnNode(ResourceNodeData data, Vector3 groundPos)
        {
            EnsureRoot();

            // GO créé inactif → AddComponent ne déclenche pas Awake immédiatement, on configure,
            // puis on active : Awake s'exécutera avec _data déjà set.
            var go = new GameObject($"{data.Id}");
            go.SetActive(false);
            go.transform.SetParent(_nodesRoot, false);
            go.transform.position = groundPos;

            // Collider pour la détection par raycast caméra. Capsule par défaut, suffisant pour le POC.
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 3f;
            col.radius = 0.6f;
            col.center = new Vector3(0f, 1.5f, 0f);

            var node = go.AddComponent<ResourceNode>();
            node.SetData(data);

            // Le composant de juice est requis par convention sur les nœuds spawned :
            // il s'abonne aux events OnHit/OnDepleted du ResourceNode pour le feedback.
            go.AddComponent<ResourceNodeJuice>();

            go.SetActive(true);
        }

        private void EnsureRoot()
        {
            if (_nodesRoot != null) return;

            var existing = transform.Find(NodesRootName);
            if (existing != null)
            {
                _nodesRoot = existing;
                return;
            }

            var go = new GameObject(NodesRootName);
            go.transform.SetParent(transform, false);
            _nodesRoot = go.transform;
        }

        private void ClearNodes()
        {
            var existing = transform.Find(NodesRootName);
            if (existing != null)
            {
                // Destroy est différé en fin de frame : on renomme d'abord pour qu'un Generate
                // enchaîné la même frame (regen runtime #74) ne retrouve PAS ce root via
                // transform.Find(NodesRootName) et n'y parente pas les nouveaux nœuds (qui
                // seraient alors détruits avec lui).
                if (Application.isPlaying)
                {
                    existing.name = NodesRootName + "_old";
                    Destroy(existing.gameObject);
                }
                else
                {
                    DestroyImmediate(existing.gameObject);
                }
            }
            _nodesRoot = null;
        }
    }
}
