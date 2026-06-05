using UnityEngine;
using UnityEngine.AI;
using Survain.Core;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Repas groupé planifié (#15) : pendant un créneau de repas (<c>WorldClock.IsMealTime</c>), le
    /// PNJ rejoint le feu de camp le plus proche, s'y attable, mange et — s'il a de la compagnie
    /// (≥1 autre PNJ proche) — gagne un bonus de moral social (événement décroissant, cf.
    /// <see cref="NpcNeeds"/>).
    ///
    /// Déclenché ET terminé par <see cref="NpcController"/> (priorité sous la nuit/faim, au-dessus
    /// du travail) : l'état ne teste pas l'heure lui-même. Distinct d'<see cref="EatingState"/>
    /// (faim individuelle, repart dès rassasié) : ici le PNJ reste au feu pour toute la durée du
    /// créneau (socialisation). Anti-blocage : timeout + repath partiel (même pattern qu'EatingState).
    /// </summary>
    public sealed class MealGatheringState : INpcState
    {
        private const float GatherRange = 3.5f;      // « attablé » sous cette distance du feu
        private const float MoveTimeout = 12f;        // si le feu reste inatteignable → mange au plus près
        private const float RepathInterval = 1.5f;    // recalcul d'un chemin partiel
        private const float SocialRadius = 4f;        // rayon de détection de compagnie

        private Transform _spot;
        private bool _gathered;
        private float _deadline;
        private float _nextRepathAt;

        public void Enter(NpcController npc)
        {
            if (!EatingState.TryFindSpot(npc.transform.position, out _spot))
            {
                npc.ChangeState(new IdleState()); // pas de feu → on vaque (retentera tant que c'est l'heure)
                return;
            }

            _gathered = false;
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
            if (_spot == null) { npc.ChangeState(new IdleState()); return; }

            var agent = npc.Agent;

            if (!_gathered)
            {
                if (Time.time > _deadline) { Gather(npc); return; } // bloqué → on s'attable sur place
                if (agent.pathPending) return;

                bool inRange = Vector3.Distance(npc.transform.position, _spot.position) <= GatherRange;
                bool settled = agent.remainingDistance <= agent.stoppingDistance + 0.2f
                               && agent.velocity.sqrMagnitude < 0.04f;

                if (!inRange && !settled)
                {
                    if (Time.time >= _nextRepathAt && agent.isOnNavMesh)
                    {
                        if (agent.pathStatus != NavMeshPathStatus.PathComplete)
                            agent.SetDestination(_spot.position);
                        _nextRepathAt = Time.time + RepathInterval;
                    }
                    return;
                }

                Gather(npc);
            }

            // Au feu : on mange et on socialise pour toute la durée du créneau (sortie pilotée par
            // NpcController quand le créneau se termine).
            if (npc.Needs != null)
            {
                npc.Needs.Feed(npc.Data.EatRatePerSecond * Time.deltaTime);
                if (HasCompany(npc))
                    npc.Needs.ApplyMoraleEvent(npc.Data.SocialMoraleBonusPerSecond * Time.deltaTime);
            }
        }

        private void Gather(NpcController npc)
        {
            if (_gathered) return;
            _gathered = true;
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = true;
            SurvainLog.Info(SurvainLog.Category.AI, $"{npc.name} s'attable pour le repas.", npc);
        }

        /// <summary>Au moins un autre PNJ se trouve à proximité (repas en compagnie).</summary>
        private static bool HasCompany(NpcController npc)
        {
            var all = NpcController.All;
            float r2 = SocialRadius * SocialRadius;
            for (int i = 0; i < all.Count; i++)
            {
                var other = all[i];
                if (other == null || other == npc) continue;
                if ((other.transform.position - npc.transform.position).sqrMagnitude <= r2)
                    return true;
            }
            return false;
        }

        public void Exit(NpcController npc)
        {
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = false;
        }
    }
}
