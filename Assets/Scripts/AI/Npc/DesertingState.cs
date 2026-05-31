using UnityEngine;
using UnityEngine.AI;
using Survain.Core;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Le PNJ au moral à zéro déserte : il s'éloigne du village (à l'opposé de son point
    /// d'origine) puis disparaît une fois parti (#13 phase 2). État terminal.
    ///
    /// Déclenché par NpcController quand NpcNeeds.IsDeserting. Au POC, la désertion = despawn ;
    /// le recrutement / remplacement de PNJ viendra en #15.
    /// </summary>
    public sealed class DesertingState : INpcState
    {
        private const float LeaveDistance = 40f;

        public void Enter(NpcController npc)
        {
            SurvainLog.Info(SurvainLog.Category.AI,
                $"{npc.name} déserte le village (moral au plus bas).", npc);

            Vector3 dir = npc.transform.position - npc.HomePosition;
            dir.y = 0f;
            if (dir.sqrMagnitude < 1f) dir = npc.transform.forward;
            Vector3 target = npc.transform.position + dir.normalized * LeaveDistance;

            if (NavMesh.SamplePosition(target, out var hit, LeaveDistance, NavMesh.AllAreas)
                && npc.Agent.isOnNavMesh)
            {
                npc.Agent.isStopped = false;
                npc.Agent.SetDestination(hit.position);
            }
        }

        public void Tick(NpcController npc)
        {
            var agent = npc.Agent;
            if (agent.pathPending) return;

            // Parti (ou chemin impossible) → le PNJ quitte définitivement la scène.
            if (agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                Object.Destroy(npc.gameObject);
            }
        }

        public void Exit(NpcController npc) { }
    }
}
