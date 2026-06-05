using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Survain.Core;
using Survain.Gameplay.Buildings;
using Survain.Items;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Spawne le village au démarrage : 1 contremaître (PNJ manager, point d'interaction unique,
    /// #14) puis quelques villageois. Place chaque PNJ sur le NavMesh (échantillonné autour d'un
    /// centre), instancie le prefab et lui injecte sa NPCData.
    ///
    /// Sert aussi de point de recrutement (#15) : <see cref="TryRecruit"/> prélève un coût dans le
    /// coffre le plus proche du contremaître et fait apparaître un villageois opérationnel
    /// (SansEmploi) — appelé par le panneau du contremaître. Accès via <see cref="Instance"/>
    /// (un seul spawner en scène) pour éviter FindObjectOfType.
    ///
    /// Ordre +200 : après NavMeshRuntimeBaker (+150) pour que le NavMesh soit prêt.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class NpcSpawner : MonoBehaviour
    {
        /// <summary>Résultat d'une tentative de recrutement (pour le feedback UI).</summary>
        public enum RecruitOutcome { Success, VillageFull, NotEnoughResources, NoStorage, Failed }

        [Tooltip("Prefab du contremaître (PNJ manager : NpcInteractable, métier Contremaître, ne déserte pas). " +
                 "Spawné une fois au démarrage. Si null, aucun contremaître n'est créé.")]
        [SerializeField] private GameObject _foremanPrefab;

        [Tooltip("Prefabs de villageois (chacun avec NpcController + NavMeshAgent + visuel Synty + Animator). " +
                 "Un prefab est tiré au hasard par PNJ spawné → village visuellement varié.")]
        [SerializeField] private GameObject[] _npcPrefabs;

        [Tooltip("Data injectée aux PNJ spawnés. Si null, on garde celle du prefab.")]
        [SerializeField] private NPCData _data;

        [Tooltip("Nombre de PNJ à spawner.")]
        [Min(0)]
        [SerializeField] private int _count = 3;

        [Tooltip("Centre de la zone de spawn. Si null = position de ce GameObject.")]
        [SerializeField] private Transform _spawnCenter;

        [Tooltip("Rayon de la zone de spawn (mètres).")]
        [Min(0f)]
        [SerializeField] private float _spawnRadius = 6f;

        [Tooltip("Distance max de recherche d'un point NavMesh (grande pour absorber la hauteur de départ du centre, ex. joueur placé au-dessus du terrain avant de tomber).")]
        [Min(1f)]
        [SerializeField] private float _navMeshSampleDistance = 100f;

        [Header("Recrutement (#15)")]
        [Tooltip("Coût prélevé dans le coffre le plus proche du contremaître pour recruter un villageois. " +
                 "Placeholder tunable (modalités en attente d'arbitrage Pascal).")]
        [SerializeField] private BuildCost[] _recruitCost;

        [Tooltip("Nombre max de villageois VIVANTS hors contremaître. Au-delà, le recrutement échoue " +
                 "(village plein) ; un déserteur libère une place. 0 = illimité.")]
        [Min(0)]
        [SerializeField] private int _maxVillagers = 8;

        /// <summary>Spawner unique en scène (set OnEnable). Consommé par le panneau de recrutement.</summary>
        public static NpcSpawner Instance { get; private set; }

        /// <summary>Coût de recrutement (lecture seule, pour l'affichage UI).</summary>
        public IReadOnlyList<BuildCost> RecruitCost => _recruitCost;

        /// <summary>Plafond de villageois vivants (0 = illimité).</summary>
        public int MaxVillagers => _maxVillagers;

        // Sac de prefabs mélangé une fois (tirage sans remise), réutilisé par le spawn initial ET
        // le recrutement → la variété visuelle continue au-delà du peuplement de départ.
        private readonly List<GameObject> _bag = new List<GameObject>();
        private int _spawnIndex;
        private Vector3 _center;

        private void OnEnable() => Instance = this;
        private void OnDisable() { if (Instance == this) Instance = null; }

        private void Start()
        {
            if (_npcPrefabs == null || _npcPrefabs.Length == 0)
            {
                SurvainLog.Error(SurvainLog.Category.AI, "NpcSpawner : aucun prefab dans _npcPrefabs.", this);
                return;
            }

            // Mélange une fois les prefabs (sans les entrées vides) : tirage sans remise → les
            // premiers PNJ sont tous distincts (pas de village de clones), puis on recycle si on
            // dépasse le nombre de modèles. La compo varie d'un lancement à l'autre.
            foreach (var p in _npcPrefabs)
                if (p != null) _bag.Add(p);

            if (_bag.Count == 0)
            {
                SurvainLog.Error(SurvainLog.Category.AI, "NpcSpawner : aucun prefab valide dans _npcPrefabs.", this);
                return;
            }

            for (int i = _bag.Count - 1; i > 0; i--) // Fisher-Yates
            {
                int j = Random.Range(0, i + 1);
                (_bag[i], _bag[j]) = (_bag[j], _bag[i]);
            }

            _center = _spawnCenter != null ? _spawnCenter.position : transform.position;

            // Le contremaître d'abord (point d'interaction unique du village).
            bool foreman = _foremanPrefab != null && SpawnNpc(_foremanPrefab, _center) != null;

            int spawned = 0;
            for (int i = 0; i < _count; i++)
                if (SpawnVillager() != null) spawned++;

            SurvainLog.Info(SurvainLog.Category.AI,
                $"NpcSpawner : {spawned}/{_count} villageois spawnés" + (foreman ? " + 1 contremaître." : "."), this);
        }

        /// <summary>Fait apparaître un villageois (prefab tiré sans remise, placé sur le NavMesh
        /// autour du centre, data injectée). Null si le placement échoue. Réutilisé par le spawn
        /// initial et le recrutement.</summary>
        public GameObject SpawnVillager()
        {
            if (_bag.Count == 0) return null;
            GameObject prefab = _bag[_spawnIndex % _bag.Count];
            _spawnIndex++;
            return SpawnNpc(prefab, _center);
        }

        /// <summary>
        /// Tente de recruter un villageois : vérifie le plafond, prélève le coût dans le coffre le
        /// plus proche de <paramref name="from"/> (le contremaître) et fait apparaître le PNJ.
        /// Le coût n'est prélevé que si le spawn réussit (pas de perte de ressources).
        /// </summary>
        public RecruitOutcome TryRecruit(Vector3 from)
        {
            // Plafond sur le nombre VIVANT (un déserteur libère une place).
            if (_maxVillagers > 0 && CountLivingVillagers() >= _maxVillagers)
                return RecruitOutcome.VillageFull;

            // Coffre le plus proche du contremaître.
            var chest = Building.FindNearest(from, b => b != null && b.GetComponent<StorageContainer>() != null);
            var inv = chest != null ? chest.GetComponent<StorageContainer>().Inventory : null;
            if (inv == null) return RecruitOutcome.NoStorage;

            // Vérifie la disponibilité du coût (sans rien prélever).
            if (_recruitCost != null)
            {
                for (int i = 0; i < _recruitCost.Length; i++)
                {
                    var c = _recruitCost[i];
                    if (c.Item != null && inv.Count(c.Item) < c.Amount)
                        return RecruitOutcome.NotEnoughResources;
                }
            }

            // Spawn d'abord : si le placement échoue, on n'a rien prélevé.
            var go = SpawnVillager();
            if (go == null) return RecruitOutcome.Failed;

            // Prélèvement effectif du coût.
            if (_recruitCost != null)
            {
                for (int i = 0; i < _recruitCost.Length; i++)
                {
                    var c = _recruitCost[i];
                    if (c.Item != null) inv.TryRemove(c.Item, c.Amount);
                }
            }

            SurvainLog.Info(SurvainLog.Category.AI, "Nouveau villageois recruté.", this);
            return RecruitOutcome.Success;
        }

        /// <summary>Nombre de villageois vivants hors contremaître.</summary>
        private static int CountLivingVillagers()
        {
            int n = 0;
            var all = NpcController.All;
            for (int i = 0; i < all.Count; i++)
            {
                var c = all[i];
                if (c != null && c.Job != NpcJob.Contremaitre) n++;
            }
            return n;
        }

        /// <summary>Place un PNJ sur le NavMesh autour du centre, l'instancie et lui injecte la data. Null si échec.</summary>
        private GameObject SpawnNpc(GameObject prefab, Vector3 center)
        {
            Vector3 candidate = center + Random.insideUnitSphere * _spawnRadius;
            candidate.y = center.y;

            if (!NavMesh.SamplePosition(candidate, out var hit, _navMeshSampleDistance, NavMesh.AllAreas))
            {
                SurvainLog.Warn(SurvainLog.Category.AI,
                    "NpcSpawner : aucun point NavMesh trouvé pour un spawn (NavMesh vide ? vérifier Use Geometry du NavMeshSurface).", this);
                return null;
            }

            var go = Instantiate(prefab, hit.position, Quaternion.identity);
            var ctrl = go.GetComponent<NpcController>();
            if (ctrl != null && _data != null) ctrl.SetData(_data);
            return go;
        }
    }
}
