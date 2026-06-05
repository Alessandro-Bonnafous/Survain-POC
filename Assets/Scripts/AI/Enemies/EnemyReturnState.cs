using UnityEngine;

namespace Survain.AI.Enemies
{
    /// <summary>
    /// Retour : après un désaggro, l'ennemi rentre vers son foyer puis reprend la patrouille.
    /// (Déclenché par <see cref="EnemyController"/> quand la cible est perdue / hors rayon / laisse.)
    /// </summary>
    public sealed class EnemyReturnState : IEnemyState
    {
        public void Enter(EnemyController e)
        {
            e.Agent.speed = e.Data.ChaseSpeed; // rentre d'un bon pas
            if (e.Agent.isOnNavMesh)
            {
                e.Agent.isStopped = false;
                e.Agent.SetDestination(e.HomePosition);
            }
        }

        public void Tick(EnemyController e)
        {
            var agent = e.Agent;
            if (agent.pathPending) return;
            if (agent.remainingDistance <= agent.stoppingDistance + 0.3f)
                e.ChangeState(new EnemyPatrolState());
        }

        public void Exit(EnemyController e) { }
    }
}
