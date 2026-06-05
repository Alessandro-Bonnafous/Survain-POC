using UnityEngine;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Le PNJ patiente sur place un temps aléatoire, puis repart errer (Wander).
    ///
    /// Idle social (#15) : si un autre PNJ oisif est tout proche, les deux se tournent l'un vers
    /// l'autre et « discutent » brièvement (petit bonus de moral social), tant que la fenêtre de
    /// chat n'est pas écoulée. Pur agrément « village vivant » — aucune interruption globale ; le
    /// comportement vit ici, dans l'oisiveté, là où le PNJ n'a rien de prioritaire à faire.
    /// </summary>
    public sealed class IdleState : INpcState
    {
        private const float ChatRadius = 3f;
        private const float MaxChatSeconds = 4f;
        private const float FaceTurnSpeed = 6f;

        private float _resumeAt;
        private float _chatUntil;

        public void Enter(NpcController npc)
        {
            if (npc.Agent.isOnNavMesh) npc.Agent.isStopped = true;
            var d = npc.Data;
            _resumeAt = Time.time + Random.Range(d.IdlePauseMin, d.IdlePauseMax);
            _chatUntil = Time.time + MaxChatSeconds;
        }

        public void Tick(NpcController npc)
        {
            // Tant que la fenêtre de chat n'est pas écoulée, on cherche un voisin oisif : on se
            // tourne vers lui et on grappille un peu de moral social. La discussion prolonge
            // l'oisiveté (on ne repart pas errer pendant qu'on parle).
            if (Time.time < _chatUntil)
            {
                var mate = FindChatMate(npc);
                if (mate != null)
                {
                    FaceTowards(npc, mate.transform.position);
                    if (npc.Needs != null)
                        npc.Needs.ApplyMoraleEvent(npc.Data.SocialMoraleBonusPerSecond * 0.5f * Time.deltaTime);
                    return;
                }
            }

            if (Time.time >= _resumeAt) npc.ChangeState(new WanderState());
        }

        public void Exit(NpcController npc) { }

        /// <summary>PNJ oisif le plus proche dans le rayon de discussion (null si personne).</summary>
        private static NpcController FindChatMate(NpcController npc)
        {
            var all = NpcController.All;
            NpcController best = null;
            float bestSqr = ChatRadius * ChatRadius;
            for (int i = 0; i < all.Count; i++)
            {
                var other = all[i];
                if (other == null || other == npc || !other.IsAvailableForChat) continue;
                float sqr = (other.transform.position - npc.transform.position).sqrMagnitude;
                if (sqr <= bestSqr) { bestSqr = sqr; best = other; }
            }
            return best;
        }

        private static void FaceTowards(NpcController npc, Vector3 target)
        {
            Vector3 dir = target - npc.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                npc.transform.rotation = Quaternion.Slerp(
                    npc.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * FaceTurnSpeed);
        }
    }
}
