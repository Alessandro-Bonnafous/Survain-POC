using UnityEngine;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Le PNJ patiente sur place un temps aléatoire, puis repart errer (Wander).
    /// </summary>
    public sealed class IdleState : INpcState
    {
        private float _resumeAt;

        public void Enter(NpcController npc)
        {
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = true;
            var d = npc.Data;
            _resumeAt = Time.time + Random.Range(d.IdlePauseMin, d.IdlePauseMax);
        }

        public void Tick(NpcController npc)
        {
            if (Time.time >= _resumeAt) npc.ChangeState(new WanderState());
        }

        public void Exit(NpcController npc) { }
    }
}
