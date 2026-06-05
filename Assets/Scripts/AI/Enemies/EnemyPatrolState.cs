using UnityEngine;
using UnityEngine.AI;

namespace Survain.AI.Enemies
{
    /// <summary>
    /// Patrouille : l'ennemi se déplace vers un point aléatoire autour de son foyer, marque une
    /// pause à l'arrivée, puis recommence. L'aggro (passage en poursuite) est gérée par
    /// <see cref="EnemyController"/>, pas ici.
    /// </summary>
    public sealed class EnemyPatrolState : IEnemyState
    {
        private bool _waiting;
        private float _resumeAt;

        public void Enter(EnemyController e)
        {
            e.Agent.speed = e.Data.PatrolSpeed;
            PickDestination(e);
        }

        public void Tick(EnemyController e)
        {
            var agent = e.Agent;

            if (_waiting)
            {
                if (Time.time >= _resumeAt) PickDestination(e);
                return;
            }

            if (agent.pathPending) return;
            if (agent.remainingDistance <= agent.stoppingDistance + 0.15f)
            {
                _waiting = true;
                if (agent.isOnNavMesh) agent.isStopped = true;
                _resumeAt = Time.time + Random.Range(e.Data.IdlePauseMin, e.Data.IdlePauseMax);
            }
        }

        public void Exit(EnemyController e) { }

        private void PickDestination(EnemyController e)
        {
            _waiting = false;
            if (!e.Agent.isOnNavMesh) return;

            e.Agent.isStopped = false;
            Vector3 candidate = e.HomePosition + Random.insideUnitSphere * e.Data.PatrolRadius;
            if (NavMesh.SamplePosition(candidate, out var hit, e.Data.PatrolRadius, NavMesh.AllAreas))
                e.Agent.SetDestination(hit.position);
        }
    }
}
