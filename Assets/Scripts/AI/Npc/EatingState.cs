using UnityEngine;
using Survain.Core;
using Survain.Gameplay.Buildings;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Le PNJ affamé va manger au feu de camp le plus proche puis repart (#13 phase 2).
    ///
    /// Déclenché par NpcController quand NpcNeeds.IsHungry (priorité sous la fuite). Le « feu de
    /// camp » est au POC tout bâtiment émettant de la lumière (BuildingData.EmitsLight) — un flag
    /// dédié pourra suivre. Sans feu de camp en scène, on ne peut pas manger → retour Idle (le PNJ
    /// reste affamé, son moral baisse). Manger ne consomme pas d'item au POC (économie plus tard).
    /// </summary>
    public sealed class EatingState : INpcState
    {
        private const float EatRange = 1.8f;       // distance au feu pour commencer à manger
        private const float FullThreshold = 0.98f; // faim considérée rassasiée

        private Transform _spot;

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
                // Pas de feu de camp → impossible de manger, on reprend une vie normale.
                npc.ChangeState(new IdleState());
                return;
            }

            if (npc.Agent.isOnNavMesh)
            {
                npc.Agent.isStopped = false;
                npc.Agent.SetDestination(_spot.position);
            }
        }

        public void Tick(NpcController npc)
        {
            if (_spot == null || npc.Needs == null)
            {
                npc.ChangeState(new IdleState());
                return;
            }

            var agent = npc.Agent;
            if (agent.pathPending) return;

            if (Vector3.Distance(npc.transform.position, _spot.position) > EatRange) return; // encore en route

            // Arrivé au feu : on mange jusqu'à rassasiement.
            agent.isStopped = true;
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
