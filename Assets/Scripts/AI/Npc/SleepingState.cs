using UnityEngine;
using UnityEngine.AI;
using Survain.Core;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Routine nocturne (#15) : le PNJ rentre à son foyer puis s'y repose jusqu'au lever du jour.
    ///
    /// Déclenché par <see cref="NpcController"/> quand <c>WorldClock.IsNight</c> (priorité sous la
    /// faim/fuite, au-dessus du travail). Le réveil (sortie d'état) est piloté par le contrôleur au
    /// retour du jour — l'état ne teste pas l'heure lui-même (cohérent avec les interruptions
    /// globales priorisées).
    ///
    /// Foyer = <see cref="NpcController.HomePosition"/> au POC (sommeil « sans lit nominal ») ;
    /// l'abri/lit assigné + <c>NpcNeeds.SetSheltered</c> viendront en #19 (Sprint 4) — il suffira
    /// d'y remplacer la destination. Pas de clip de sommeil dédié encore → pose Idle (agent stoppé).
    /// Anti-blocage : timeout + repath si le chemin devient partiel (même pattern qu'EatingState).
    /// </summary>
    public sealed class SleepingState : INpcState
    {
        private const float HomeRange = 1.5f;        // « arrivé au foyer » sous cette distance
        private const float MoveTimeout = 12f;       // si le foyer reste inatteignable → dort sur place
        private const float RepathInterval = 1.5f;   // recalcul périodique d'un chemin partiel

        private Vector3 _home;
        private bool _resting;
        private float _deadline;
        private float _nextRepathAt;

        public void Enter(NpcController npc)
        {
            _home = npc.HomePosition;
            _resting = false;
            _deadline = Time.time + MoveTimeout;
            _nextRepathAt = Time.time + RepathInterval;

            if (npc.Agent.isOnNavMesh)
            {
                npc.Agent.isStopped = false;
                npc.Agent.SetDestination(_home);
            }
        }

        public void Tick(NpcController npc)
        {
            if (_resting) return; // dort jusqu'au réveil (piloté par NpcController au retour du jour)

            var agent = npc.Agent;

            if (Time.time > _deadline) { Rest(npc); return; } // bloqué → on se repose sur place
            if (agent.pathPending) return;

            bool inRange = Vector3.Distance(npc.transform.position, _home) <= HomeRange;
            bool settled = agent.remainingDistance <= agent.stoppingDistance + 0.2f
                           && agent.velocity.sqrMagnitude < 0.04f;

            if (!inRange && !settled)
            {
                // Recalcule SEULEMENT si le chemin est partiel/invalide (évite le micro-stutter d'un
                // repath systématique pendant un trajet normal).
                if (Time.time >= _nextRepathAt && agent.isOnNavMesh)
                {
                    if (agent.pathStatus != NavMeshPathStatus.PathComplete)
                        agent.SetDestination(_home);
                    _nextRepathAt = Time.time + RepathInterval;
                }
                return;
            }

            Rest(npc);
        }

        private void Rest(NpcController npc)
        {
            if (_resting) return;
            _resting = true;
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = true;
            SurvainLog.Info(SurvainLog.Category.AI, $"{npc.name} se repose pour la nuit.", npc);
        }

        public void Exit(NpcController npc)
        {
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = false;
        }
    }
}
