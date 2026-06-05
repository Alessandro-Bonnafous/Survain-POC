using UnityEngine;
using UnityEngine.AI;
using Survain.Core;

namespace Survain.AI.Enemies
{
    /// <summary>
    /// Spawne des ennemis au démarrage, placés sur le NavMesh autour d'un centre (même pattern que
    /// NpcSpawner). Phase 1 : un type d'ennemi + un nombre fixe. Phase 2 : plusieurs types (loup/
    /// troll/bandit), densité par zone et respawn — et la zone sauvage (#18) fournira les centres.
    ///
    /// Ordre +200 : après NavMeshRuntimeBaker (+150) pour que le NavMesh soit prêt.
    /// Prérequis prefab : root sur le layer Threat (pour que les PNJ le fuient) + collider +
    /// NavMeshAgent + EnemyController.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class EnemySpawner : MonoBehaviour
    {
        [Tooltip("Prefab d'ennemi (EnemyController + NavMeshAgent + collider + visuel, root sur layer Threat).")]
        [SerializeField] private GameObject _enemyPrefab;

        [Tooltip("Data injectée aux ennemis spawnés. Si null, on garde celle du prefab.")]
        [SerializeField] private EnemyData _data;

        [Tooltip("Nombre d'ennemis à spawner.")]
        [Min(0)] [SerializeField] private int _count = 3;

        [Tooltip("Centre de la zone de spawn. Si null = position de ce GameObject.")]
        [SerializeField] private Transform _spawnCenter;

        [Tooltip("Rayon de la zone de spawn (mètres).")]
        [Min(0f)] [SerializeField] private float _spawnRadius = 12f;

        [Tooltip("Distance max de recherche d'un point NavMesh.")]
        [Min(1f)] [SerializeField] private float _navMeshSampleDistance = 100f;

        private void Start()
        {
            if (_enemyPrefab == null)
            {
                SurvainLog.Error(SurvainLog.Category.AI, "EnemySpawner : _enemyPrefab non assigné.", this);
                return;
            }

            Vector3 center = _spawnCenter != null ? _spawnCenter.position : transform.position;
            int spawned = 0;
            for (int i = 0; i < _count; i++)
                if (Spawn(center) != null) spawned++;

            SurvainLog.Info(SurvainLog.Category.AI, $"EnemySpawner : {spawned}/{_count} ennemis spawnés.", this);
        }

        private GameObject Spawn(Vector3 center)
        {
            Vector3 candidate = center + Random.insideUnitSphere * _spawnRadius;
            candidate.y = center.y;

            if (!NavMesh.SamplePosition(candidate, out var hit, _navMeshSampleDistance, NavMesh.AllAreas))
            {
                SurvainLog.Warn(SurvainLog.Category.AI,
                    "EnemySpawner : aucun point NavMesh trouvé pour un spawn.", this);
                return null;
            }

            var go = Instantiate(_enemyPrefab, hit.position, Quaternion.identity);
            var ctrl = go.GetComponent<EnemyController>();
            if (ctrl != null && _data != null) ctrl.SetData(_data);
            return go;
        }
    }
}
