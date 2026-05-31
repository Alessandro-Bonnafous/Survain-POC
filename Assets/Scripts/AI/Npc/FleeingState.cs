using UnityEngine;
using UnityEngine.AI;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Le PNJ fuit la menace perçue (NpcPerception) : il court à l'opposé, re-visant un point de
    /// fuite régulièrement tant que la menace le suit, puis repasse en Idle quand elle a disparu.
    ///
    /// État prioritaire : NpcController le déclenche globalement dès qu'une menace est perçue,
    /// en interruption de n'importe quel autre état (cf. NpcController.Update).
    /// </summary>
    public sealed class FleeingState : INpcState
    {
        private const float FleeDistance = 10f;     // distance du point de fuite visé (mètres)
        private const float RetargetEvery = 0.5f;   // re-visée du point de fuite (secondes)

        private float _retargetAt;

        public void Enter(NpcController npc)
        {
            npc.Agent.speed = npc.Data.FleeSpeed; // course
            PickFleePoint(npc);
        }

        public void Tick(NpcController npc)
        {
            // Plus de menace → on se calme.
            if (npc.Perception == null || !npc.Perception.HasThreat)
            {
                npc.ChangeState(new IdleState());
                return;
            }

            // La menace bouge → on re-vise un point de fuite périodiquement.
            if (Time.time >= _retargetAt) PickFleePoint(npc);
        }

        public void Exit(NpcController npc)
        {
            // Restaure la vitesse d'errance pour les états suivants.
            if (npc.Data != null) npc.Agent.speed = npc.Data.MoveSpeed;
        }

        private void PickFleePoint(NpcController npc)
        {
            _retargetAt = Time.time + RetargetEvery;

            Vector3 away = npc.transform.position - npc.Perception.ThreatPosition;
            away.y = 0f;
            if (away.sqrMagnitude < 0.01f) away = npc.transform.forward; // menace pile dessus
            Vector3 target = npc.transform.position + away.normalized * FleeDistance;

            if (NavMesh.SamplePosition(target, out var hit, FleeDistance, NavMesh.AllAreas)
                && npc.Agent.isOnNavMesh)
            {
                npc.Agent.isStopped = false;
                npc.Agent.SetDestination(hit.position);
            }
        }
    }
}
