using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Survain.Core;

namespace Survain.AI.Enemies
{
    /// <summary>
    /// Spawne et entretient une population d'ennemis autour d'un centre, sur le NavMesh (même
    /// pattern que NpcSpawner). Plusieurs types possibles (loup/troll/bandit via plusieurs
    /// EnemyData, tirés au hasard par spawn → variété). À la mort d'un ennemi, un remplaçant
    /// réapparaît après un délai (maintien de la densité).
    ///
    /// Ordre +200 : après NavMeshRuntimeBaker (+150). Prérequis prefab : root sur layer Threat +
    /// collider + NavMeshAgent + EnemyController. La zone sauvage (#18) fournira plusieurs centres.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(200)]
    public sealed class EnemySpawner : MonoBehaviour
    {
        [Tooltip("Prefab d'ennemi (root sur layer Threat, collider, NavMeshAgent, EnemyController).")]
        [SerializeField] private GameObject _enemyPrefab;

        [Tooltip("Types d'ennemis possibles (loup/troll/bandit…). Un type est tiré au hasard par spawn.")]
        [SerializeField] private EnemyData[] _enemyTypes;

        [Tooltip("Nombre d'ennemis maintenus en vie (densité).")]
        [Min(0)] [SerializeField] private int _count = 3;

        [Tooltip("Centre de la zone de spawn. Si null = position de ce GameObject.")]
        [SerializeField] private Transform _spawnCenter;

        [Tooltip("Rayon de la zone de spawn (mètres).")]
        [Min(0f)] [SerializeField] private float _spawnRadius = 12f;

        [Tooltip("Distance max de recherche d'un point NavMesh.")]
        [Min(1f)] [SerializeField] private float _navMeshSampleDistance = 100f;

        [Header("Respawn")]
        [Tooltip("Si vrai, un ennemi tué est remplacé après un délai (maintien de la densité).")]
        [SerializeField] private bool _respawn = true;

        [Tooltip("Délai avant réapparition d'un remplaçant (secondes).")]
        [Min(0f)] [SerializeField] private float _respawnDelaySeconds = 8f;

        private readonly List<EnemyData> _types = new List<EnemyData>();
        private Vector3 _center;

        private void Start()
        {
            if (_enemyPrefab == null)
            {
                SurvainLog.Error(SurvainLog.Category.AI, "EnemySpawner : _enemyPrefab non assigné.", this);
                return;
            }

            foreach (var t in _enemyTypes)
                if (t != null) _types.Add(t);

            if (_types.Count == 0)
            {
                SurvainLog.Error(SurvainLog.Category.AI, "EnemySpawner : aucun EnemyData valide dans _enemyTypes.", this);
                return;
            }

            _center = _spawnCenter != null ? _spawnCenter.position : transform.position;

            int spawned = 0;
            for (int i = 0; i < _count; i++)
                if (Spawn() != null) spawned++;

            SurvainLog.Info(SurvainLog.Category.AI, $"EnemySpawner : {spawned}/{_count} ennemis spawnés.", this);
        }

        private GameObject Spawn()
        {
            Vector3 candidate = _center + Random.insideUnitSphere * _spawnRadius;
            candidate.y = _center.y;

            if (!NavMesh.SamplePosition(candidate, out var hit, _navMeshSampleDistance, NavMesh.AllAreas))
            {
                SurvainLog.Warn(SurvainLog.Category.AI, "EnemySpawner : aucun point NavMesh trouvé pour un spawn.", this);
                return null;
            }

            var go = Instantiate(_enemyPrefab, hit.position, Quaternion.identity);
            var ctrl = go.GetComponent<EnemyController>();
            if (ctrl != null)
            {
                ctrl.SetData(_types[Random.Range(0, _types.Count)]); // tirage du type avant Start
                ctrl.Died += OnEnemyDied;                            // pour le respawn
            }
            return go;
        }

        private void OnEnemyDied(EnemyController e)
        {
            e.Died -= OnEnemyDied;
            if (_respawn && isActiveAndEnabled) StartCoroutine(RespawnAfterDelay());
        }

        private IEnumerator RespawnAfterDelay()
        {
            yield return new WaitForSeconds(_respawnDelaySeconds);
            Spawn();
        }
    }
}
