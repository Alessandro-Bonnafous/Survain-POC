using UnityEngine;

namespace Survain.AI.Enemies
{
    /// <summary>
    /// Poursuite : l'ennemi fonce vers sa cible (chemin recalculé périodiquement). À portée
    /// d'attaque, il passe en <see cref="EnemyAttackState"/>. Le désaggro (cible perdue / hors
    /// rayon / laisse) est géré par <see cref="EnemyController"/>.
    /// </summary>
    public sealed class EnemyChaseState : IEnemyState
    {
        private const float RepathInterval = 0.25f; // suit une cible mobile sans repath chaque frame
        private float _nextRepathAt;

        public void Enter(EnemyController e)
        {
            e.Agent.speed = e.Data.ChaseSpeed;
            if (e.Agent.isOnNavMesh) e.Agent.isStopped = false;
            _nextRepathAt = 0f;
        }

        public void Tick(EnemyController e)
        {
            var target = e.Target;
            if (target == null) return; // EnemyController basculera en Return à la frame suivante

            if (EnemyController.PlanarDistance(target.position, e.transform.position) <= e.Data.AttackRange)
            {
                e.ChangeState(new EnemyAttackState());
                return;
            }

            if (Time.time >= _nextRepathAt && e.Agent.isOnNavMesh)
            {
                e.Agent.SetDestination(target.position);
                _nextRepathAt = Time.time + RepathInterval;
            }
        }

        public void Exit(EnemyController e) { }
    }
}
