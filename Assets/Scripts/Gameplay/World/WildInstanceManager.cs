using UnityEngine;
using Survain.Core;
using Survain.AI.Enemies;
using Survain.AI.Npc;
using Survain.Gameplay.Items;
using Survain.Gameplay.Player;

namespace Survain.Gameplay.World
{
    /// <summary>
    /// Orchestre la zone sauvage comme une **instance** régénérée à l'accès (#74, version B :
    /// régénération in-place, mono-scène). Posé sur le root <c>_WildZone</c>.
    ///
    /// - <see cref="EnterWild"/> (appelé par le PNJ portail) : régénère l'instance (nouveau seed →
    ///   terrain → ressources → rebake NavMesh → reset ennemis) PUIS téléporte le joueur à l'entrée.
    ///   **Sauf si une tombe est présente dans la zone** : on préserve alors le layout pour que le
    ///   joueur récupère son loot (arbitrage). La régén reprend une fois la tombe vidée/expirée.
    /// - <see cref="ExitWild"/> (portail de sortie) : ramène le joueur au village (sans régén).
    /// - Une **barrière** périmétrique (colliders invisibles) bloque le passage à pied → l'accès se
    ///   fait uniquement par le portail (arbitrage « portail uniquement »).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WildInstanceManager : MonoBehaviour
    {
        [Header("Générateurs de l'instance")]
        [Tooltip("TerrainGenerator de la zone sauvage.")]
        [SerializeField] private TerrainGenerator _wildTerrain;

        [Tooltip("ResourceNodeSpawner de la zone sauvage.")]
        [SerializeField] private ResourceNodeSpawner _wildResources;

        [Tooltip("Baker NavMesh (surface unique) à rebaker après régénération.")]
        [SerializeField] private NavMeshRuntimeBaker _navBaker;

        [Header("Points de téléportation")]
        [Tooltip("Où le joueur apparaît dans l'instance (entrée).")]
        [SerializeField] private Transform _entrance;

        [Tooltip("Où le joueur revient au village (sortie).")]
        [SerializeField] private Transform _villageReturn;

        [Header("Barrière (accès portail uniquement)")]
        [Tooltip("Hauteur des murs invisibles (m).")]
        [SerializeField] private float _barrierHeight = 20f;

        [Tooltip("Épaisseur des murs invisibles (m).")]
        [SerializeField] private float _barrierThickness = 2f;

        private EnemySpawner[] _enemySpawners;
        private bool _barrierBuilt;

        private void Awake()
        {
            // Les spawners d'ennemis sont sous _WildZone → auto-résolus (pas de FindObjectOfType).
            _enemySpawners = GetComponentsInChildren<EnemySpawner>(includeInactive: true);
        }

        private void Start()
        {
            EnsureBarrier(); // bloque le passage à pied dès le départ (le terrain existe déjà au Start)
        }

        // ─── API publique ───────────────────────────────────────────────────

        /// <summary>Entre dans l'instance : régénère (sauf si une tombe y est) puis téléporte.</summary>
        [ContextMenu("DEBUG / Entrer en zone sauvage")]
        public void EnterWild()
        {
            if (AnyGraveInWild())
                SurvainLog.Info(SurvainLog.Category.World,
                    "Instance préservée (tombe à récupérer) : pas de régénération.", this);
            else
                RegenerateInstance();

            TeleportPlayer(_entrance, "entrée de la zone sauvage");
        }

        /// <summary>Quitte l'instance (retour village). Pas de régénération (elle aura lieu à la
        /// prochaine entrée si aucune tombe ne reste).</summary>
        [ContextMenu("DEBUG / Sortir de la zone sauvage")]
        public void ExitWild()
        {
            TeleportPlayer(_villageReturn, "village");
        }

        // ─── Régénération ───────────────────────────────────────────────────

        [ContextMenu("DEBUG / Régénérer l'instance")]
        public void RegenerateInstance()
        {
            if (_wildTerrain == null)
            {
                SurvainLog.Error(SurvainLog.Category.World, "WildInstanceManager : _wildTerrain non assigné.", this);
                return;
            }

            int seed = NewSeed();
            SurvainLog.Info(SurvainLog.Category.World, $"Régénération de l'instance sauvage (seed={seed}).", this);

            _wildTerrain.GenerateWithSeed(seed);
            if (_wildResources != null) _wildResources.GenerateWithSeed(seed);
            if (_navBaker != null) _navBaker.Rebake();

            if (_enemySpawners != null)
                for (int i = 0; i < _enemySpawners.Length; i++)
                    if (_enemySpawners[i] != null) _enemySpawners[i].ResetSpawns();

            EnsureBarrier();
        }

        private static int NewSeed() => Random.Range(1, int.MaxValue);

        // ─── Préservation de la tombe ───────────────────────────────────────

        /// <summary>Vrai si au moins une tombe du joueur se trouve dans l'emprise (XZ) de la zone
        /// sauvage → on ne régénère pas (le joueur doit pouvoir récupérer son loot).</summary>
        private bool AnyGraveInWild()
        {
            Bounds b = WildBounds();
            var graves = Grave.All;
            for (int i = 0; i < graves.Count; i++)
            {
                var g = graves[i];
                if (g == null) continue;
                Vector3 p = g.transform.position;
                if (p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z)
                    return true;
            }
            return false;
        }

        private Bounds WildBounds()
        {
            var mc = _wildTerrain != null ? _wildTerrain.GetComponent<MeshCollider>() : null;
            if (mc != null && mc.sharedMesh != null) return mc.bounds;
            return new Bounds(transform.position, new Vector3(100f, 50f, 100f));
        }

        // ─── Barrière périmétrique ──────────────────────────────────────────

        private void EnsureBarrier()
        {
            if (_barrierBuilt) return;
            Bounds b = WildBounds();
            if (b.size.x < 1f || b.size.z < 1f) return; // terrain pas encore généré

            var root = new GameObject("WildBarrier");
            root.transform.SetParent(transform, worldPositionStays: true);

            float h = _barrierHeight;
            float t = _barrierThickness;
            Vector3 c = b.center;
            CreateWall(root.transform, "Wall_+X", new Vector3(b.max.x, c.y, c.z), new Vector3(t, h, b.size.z + t));
            CreateWall(root.transform, "Wall_-X", new Vector3(b.min.x, c.y, c.z), new Vector3(t, h, b.size.z + t));
            CreateWall(root.transform, "Wall_+Z", new Vector3(c.x, c.y, b.max.z), new Vector3(b.size.x + t, h, t));
            CreateWall(root.transform, "Wall_-Z", new Vector3(c.x, c.y, b.min.z), new Vector3(b.size.x + t, h, t));

            _barrierBuilt = true;
            SurvainLog.Info(SurvainLog.Category.World, "Barrière de zone sauvage créée (accès portail uniquement).", this);
        }

        private static void CreateWall(Transform parent, string name, Vector3 worldPos, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: true);
            go.transform.position = worldPos;
            var box = go.AddComponent<BoxCollider>();
            box.size = size; // collider invisible (pas de Renderer)
        }

        // ─── Téléportation ──────────────────────────────────────────────────

        private static void TeleportPlayer(Transform target, string label)
        {
            if (target == null)
            {
                SurvainLog.Error(SurvainLog.Category.World, $"WildInstanceManager : point de téléportation '{label}' non assigné.");
                return;
            }
            var player = PlayerController.Instance;
            if (player == null) return;
            player.Teleport(target.position);
            SurvainLog.Info(SurvainLog.Category.World, $"Joueur téléporté vers : {label}.");
        }
    }
}
