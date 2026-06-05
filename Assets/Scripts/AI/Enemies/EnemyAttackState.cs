using UnityEngine;
using Survain.Core;

namespace Survain.AI.Enemies
{
    /// <summary>
    /// Attaque : l'ennemi s'arrête, fait face à la cible et enchaîne des frappes (telegraph +
    /// cooldown). **Phase 1 : la frappe ne fait aucun dégât** (vie joueur = #19, combat = #16) —
    /// c'est un windup visible qui complète la boucle Patrol→Detect→Chase→Attack→Return. Si la
    /// cible sort de portée, on repasse en poursuite ; le désaggro reste géré par EnemyController.
    /// </summary>
    public sealed class EnemyAttackState : IEnemyState
    {
        private const float FaceTurnSpeed = 8f;

        private bool _striking;
        private float _telegraphEndAt;
        private float _nextAttackAt;

        public void Enter(EnemyController e)
        {
            if (e.Agent.isOnNavMesh) e.Agent.isStopped = true;
            BeginStrike(e);
        }

        public void Tick(EnemyController e)
        {
            var target = e.Target;
            if (target == null) return; // EnemyController → Return

            FaceTarget(e, target.position);

            // Cible qui s'éloigne (avec un peu d'hystérésis) → reprise de la poursuite.
            if (EnemyController.PlanarDistance(target.position, e.transform.position) > e.Data.AttackRange * 1.2f)
            {
                e.ChangeState(new EnemyChaseState());
                return;
            }

            if (_striking)
            {
                if (Time.time >= _telegraphEndAt)
                {
                    // Frappe résolue. Phase 1 : pas de dégâts (à brancher avec #16 / #19).
                    _striking = false;
                    _nextAttackAt = Time.time + e.Data.AttackCooldown;
                    SurvainLog.Info(SurvainLog.Category.AI, $"{e.name} frappe (telegraph, sans dégâts).", e);
                }
                return;
            }

            if (Time.time >= _nextAttackAt) BeginStrike(e);
        }

        public void Exit(EnemyController e)
        {
            if (e.Agent.isOnNavMesh) e.Agent.isStopped = false;
        }

        private void BeginStrike(EnemyController e)
        {
            _striking = true;
            _telegraphEndAt = Time.time + e.Data.AttackTelegraphSeconds;
            // (Déclenchement d'une anim d'attaque à brancher avec le visuel en #16.)
        }

        private static void FaceTarget(EnemyController e, Vector3 targetPos)
        {
            Vector3 dir = targetPos - e.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                e.transform.rotation = Quaternion.Slerp(
                    e.transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * FaceTurnSpeed);
        }
    }
}
