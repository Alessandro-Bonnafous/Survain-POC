using UnityEngine;
using UnityEngine.AI;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Le PNJ choisit un point aléatoire sur le NavMesh autour de son point d'origine et s'y
    /// rend. Arrivé, il repasse en Idle. Si aucun point valide n'est trouvé, retour Idle.
    /// </summary>
    public sealed class WanderState : INpcState
    {
        public void Enter(NpcController npc)
        {
            var d = npc.Data;
            Vector3 candidate = npc.HomePosition + Random.insideUnitSphere * d.WanderRadius;

            if (NavMesh.SamplePosition(candidate, out var hit, d.WanderRadius, NavMesh.AllAreas)
                && npc.Agent.isOnNavMesh)
            {
                npc.Agent.isStopped = false;
                npc.Agent.SetDestination(hit.position);
            }
            else
            {
                npc.ChangeState(new IdleState());
            }
        }

        public void Tick(NpcController npc)
        {
            var agent = npc.Agent;
            if (agent.pathPending) return;
            // Arrivé (ou chemin invalide) → on patiente.
            if (agent.remainingDistance <= agent.stoppingDistance + 0.15f)
            {
                npc.ChangeState(new IdleState());
            }
        }

        public void Exit(NpcController npc) { }
    }
}
