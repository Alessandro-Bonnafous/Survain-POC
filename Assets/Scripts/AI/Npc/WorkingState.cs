using UnityEngine;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Le PNJ travaille sur place (anim de travail via le paramètre "isWorking").
    ///
    /// Squelette extensible (#12) : l'entrée/sortie sera pilotée par le métier en #14 — le
    /// bûcheron récolte un nœud, le constructeur alimente un ConstructionSite. Au POC, l'état
    /// stoppe l'agent et joue l'anim de travail jusqu'à un ChangeState externe (ou ContextMenu
    /// DEBUG sur NpcController). La cible de travail (nœud, chantier) viendra avec #14.
    /// </summary>
    public sealed class WorkingState : INpcState
    {
        public void Enter(NpcController npc)
        {
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = true;
            if (npc.Animator != null) npc.Animator.SetBool(NpcAnimParams.Working, true);
        }

        public void Tick(NpcController npc) { }

        public void Exit(NpcController npc)
        {
            if (npc.Animator != null) npc.Animator.SetBool(NpcAnimParams.Working, false);
        }
    }
}
