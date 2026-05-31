using UnityEngine;
using UnityEngine.AI;
using Survain.Core;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Spawne quelques PNJ au démarrage pour tester (#12). Place chaque PNJ sur le NavMesh
    /// (échantillonné autour d'un centre), instancie le prefab et lui injecte sa NPCData.
    ///
    /// Ordre +200 : après NavMeshRuntimeBaker (+150) pour que le NavMesh soit prêt.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class NpcSpawner : MonoBehaviour
    {
        [Tooltip("Prefab du PNJ (avec NpcController + NavMeshAgent + visuel + Animator).")]
        [SerializeField] private GameObject _npcPrefab;

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
            if (_npcPrefab == null)
            {
                SurvainLog.Error(SurvainLog.Category.AI, "NpcSpawner : _npcPrefab non assigné.", this);
                return;
            }

            Vector3 center = _spawnCenter != null ? _spawnCenter.position : transform.position;
            int spawned = 0;

            for (int i = 0; i < _count; i++)
            {
                Vector3 candidate = center + Random.insideUnitSphere * _spawnRadius;
                candidate.y = center.y;

                if (!NavMesh.SamplePosition(candidate, out var hit, _navMeshSampleDistance, NavMesh.AllAreas))
                {
                    SurvainLog.Warn(SurvainLog.Category.AI,
                        "NpcSpawner : aucun point NavMesh trouvé pour un spawn (NavMesh vide ? vérifier Use Geometry du NavMeshSurface).", this);
                    continue;
                }

                var go = Instantiate(_npcPrefab, hit.position, Quaternion.identity);
                var ctrl = go.GetComponent<NpcController>();
                if (ctrl != null && _data != null) ctrl.SetData(_data);
                spawned++;
            }

            SurvainLog.Info(SurvainLog.Category.AI, $"NpcSpawner : {spawned}/{_count} PNJ spawnés.", this);
        }
    }
}
