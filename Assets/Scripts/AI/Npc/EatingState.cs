using UnityEngine;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Le PNJ mange sur place (restaure la faim).
    ///
    /// Squelette extensible (#12) : l'entrée sera déclenchée par le système de besoins en #13
    /// (faim sous un seuil → aller manger), la sortie quand la faim est rassasiée. Au POC,
    /// l'état stoppe l'agent ; pas de clip Mixamo dédié encore → pose Idle en attendant (vrai
    /// clip plus tard). Pilotable via ContextMenu DEBUG sur NpcController pour valider la transition.
    /// </summary>
    public sealed class EatingState : INpcState
    {
        public void Enter(NpcController npc)
        {
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = true;
        }

        public void Tick(NpcController npc) { }

        public void Exit(NpcController npc) { }
    }
}
