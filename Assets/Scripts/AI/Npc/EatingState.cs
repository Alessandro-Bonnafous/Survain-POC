using UnityEngine;
using UnityEngine.AI;
using Survain.Core;
using Survain.Gameplay.Buildings;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Le PNJ affamé va manger au feu de camp le plus proche puis repart (#13 phase 2).
    ///
    /// Déclenché par NpcController quand NpcNeeds.IsHungry (priorité sous la fuite). Le « feu de
    /// camp » est au POC tout bâtiment émettant de la lumière (BuildingData.EmitsLight). Sans feu
    /// en scène → retour Idle. Manger ne consomme pas d'item au POC.
    ///
    /// Robustesse déplacement : le chemin vers le feu est recalculé périodiquement (un chemin
    /// peut devenir partiel/périmé — ex. un nœud qui respawn et carve le NavMesh) et un timeout
    /// évite tout blocage permanent (le PNJ abandonne puis retentera). « Arrivé » = dans la portée
    /// OU bloqué au plus près par la foule autour du feu → on mange sur place.
    /// </summary>
    public sealed class EatingState : INpcState
    {
        private const float EatRange = 3.5f;        // portée de repas (large : plusieurs PNJ autour)
        private const float FullThreshold = 0.98f;  // faim considérée rassasiée
        private const float MoveTimeout = 12f;       // abandon si le feu reste inatteignable
        private const float RepathInterval = 1.5f;   // recalcul périodique du chemin

        private Transform _spot;
        private bool _eating;
        private float _deadline;
        private float _nextRepathAt;

        /// <summary>Feu de camp le plus proche (bâtiment lumineux). Centralise le prédicat « repas ».</summary>
        public static bool TryFindSpot(Vector3 from, out Transform spot)
        {
            var fire = Building.FindNearest(from, b => b.Data != null && b.Data.EmitsLight);
            spot = fire != null ? fire.transform : null;
            return spot != null;
        }

        public void Enter(NpcController npc)
        {
            if (!TryFindSpot(npc.transform.position, out _spot))
            {
                npc.ChangeState(new IdleState()); // pas de feu → on reprend une vie normale
                return;
            }

            _eating = false;
            _deadline = Time.time + MoveTimeout;
            _nextRepathAt = Time.time + RepathInterval;
            if (npc.Agent.isOnNavMesh)
            {
                npc.Agent.isStopped = false;
                npc.Agent.SetDestination(_spot.position);
            }
        }

        public void Tick(NpcController npc)
        {
            if (_spot == null || npc.Needs == null) { npc.ChangeState(new IdleState()); return; }

            var agent = npc.Agent;

            if (!_eating)
            {
                // Anti-blocage : si le feu reste inatteignable, on abandonne (le PNJ retentera).
                if (Time.time > _deadline) { npc.ChangeState(new IdleState()); return; }
                if (agent.pathPending) return;

                bool inRange = Vector3.Distance(npc.transform.position, _spot.position) <= EatRange;
                bool settled = agent.remainingDistance <= agent.stoppingDistance + 0.2f
                               && agent.velocity.sqrMagnitude < 0.04f;

                if (!inRange && !settled)
                {
                    // Recalcule le chemin SEULEMENT s'il est partiel/invalide (obstacle apparu) →
                    // évite le micro-stutter d'un repath systématique pendant un trajet normal.
                    if (Time.time >= _nextRepathAt && agent.isOnNavMesh)
                    {
                        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
                            agent.SetDestination(_spot.position);
                        _nextRepathAt = Time.time + RepathInterval;
                    }
                    return;
                }

                _eating = true;
                if (agent.isOnNavMesh) agent.isStopped = true;
            }

            // Au feu : on mange jusqu'à rassasiement.
            npc.Needs.Feed(npc.Data.EatRatePerSecond * Time.deltaTime);
            if (npc.Needs.Hunger >= FullThreshold)
            {
                SurvainLog.Info(SurvainLog.Category.AI, $"{npc.name} a fini de manger.", npc);
                npc.ChangeState(new IdleState());
            }
        }

        public void Exit(NpcController npc)
        {
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = false;
        }
    }
}
