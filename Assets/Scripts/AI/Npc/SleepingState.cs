using UnityEngine;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Le PNJ dort sur place (restaure le repos / lié au cycle jour-nuit).
    ///
    /// Squelette extensible (#12) : l'entrée sera déclenchée par les routines quotidiennes en #15
    /// (nuit + à l'abri → dormir), la sortie au lever du jour. Au POC, l'état stoppe l'agent ;
    /// pas de clip Mixamo dédié encore → pose Idle en attendant (vrai clip plus tard). Pilotable
    /// via ContextMenu DEBUG sur NpcController pour valider la transition.
    /// </summary>
    public sealed class SleepingState : INpcState
    {
        public void Enter(NpcController npc)
        {
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = true;
        }

        public void Tick(NpcController npc) { }

        public void Exit(NpcController npc) { }
    }
}
