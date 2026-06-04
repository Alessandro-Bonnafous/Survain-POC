using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Survain.Core;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Spawne le village au démarrage : 1 contremaître (PNJ manager, point d'interaction unique,
    /// #14) puis quelques villageois. Place chaque PNJ sur le NavMesh (échantillonné autour d'un
    /// centre), instancie le prefab et lui injecte sa NPCData.
    ///
    /// Ordre +200 : après NavMeshRuntimeBaker (+150) pour que le NavMesh soit prêt.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class NpcSpawner : MonoBehaviour
    {
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

        private void Start()
        {
            if (_npcPrefabs == null || _npcPrefabs.Length == 0)
            {
                SurvainLog.Error(SurvainLog.Category.AI, "NpcSpawner : aucun prefab dans _npcPrefabs.", this);
                return;
            }

            // Mélange une fois les prefabs (sans les entrées vides) : tirage sans remise → les
            // premiers PNJ sont tous distincts (pas de village de clones), puis on recycle si
            // _count dépasse le nombre de modèles. La compo varie d'un lancement à l'autre.
            var bag = new List<GameObject>(_npcPrefabs.Length);
            foreach (var p in _npcPrefabs)
                if (p != null) bag.Add(p);

            if (bag.Count == 0)
            {
                SurvainLog.Error(SurvainLog.Category.AI, "NpcSpawner : aucun prefab valide dans _npcPrefabs.", this);
                return;
            }

            for (int i = bag.Count - 1; i > 0; i--) // Fisher-Yates
            {
                int j = Random.Range(0, i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }

            Vector3 center = _spawnCenter != null ? _spawnCenter.position : transform.position;

            // Le contremaître d'abord (point d'interaction unique du village).
            bool foreman = _foremanPrefab != null && SpawnNpc(_foremanPrefab, center) != null;

            int spawned = 0;
            for (int i = 0; i < _count; i++)
            {
                GameObject prefab = bag[spawned % bag.Count]; // sans remise tant que possible
                if (SpawnNpc(prefab, center) != null) spawned++;
            }

            SurvainLog.Info(SurvainLog.Category.AI,
                $"NpcSpawner : {spawned}/{_count} villageois spawnés" + (foreman ? " + 1 contremaître." : "."), this);
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
