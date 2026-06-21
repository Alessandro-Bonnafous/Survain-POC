using UnityEngine;
using Survain.Core;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Relais d'Animation Events (combat #16). Les Animation Events sont déclenchés sur le GameObject
    /// qui porte l'<see cref="Animator"/> — l'<b>avatar enfant</b> du _Player — alors que la logique de
    /// combat vit sur <see cref="PlayerEnemyStrike"/> (racine _Player). Ce composant, posé sur l'avatar,
    /// reçoit les events et les <b>fait remonter</b> à la frappe (résolue via <c>GetComponentInParent</c>).
    ///
    /// <para>À câbler côté Unity : poser ce composant sur l'avatar (celui qui a l'Animator), puis ajouter
    /// sur les clips d'attaque (Chop/Mine, et plus tard les compétences) deux Animation Events :
    /// <list type="bullet">
    /// <item><b><see cref="AnimImpact"/></b> à la <b>frame de contact</b> (la hache/pioche touche) ;</item>
    /// <item><b><see cref="AnimSwingEnd"/></b> en <b>fin de clip</b> (déverrouille l'enchaînement).</item>
    /// </list>
    /// Tant que les events ne sont pas posés, <see cref="PlayerEnemyStrike"/> retombe sur un filet de
    /// sécurité temporisé (le jeu reste fonctionnel, juste non synchro).</para>
    ///
    /// <para>Note : les clips Chop/Mine servent aussi à la récolte. Les events s'y déclenchent donc aussi
    /// pendant une récolte, mais <see cref="PlayerEnemyStrike"/> les ignore s'il n'a pas de swing de combat
    /// en cours (garde <c>IsSwinging</c>) — pas d'effet de bord.</para>
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerAttackAnimationRelay : MonoBehaviour
    {
        [Tooltip("Frappe de combat à notifier. Auto-résolue dans les parents si laissée vide.")]
        [SerializeField] private PlayerEnemyStrike _strike;

        private bool _warned;

        private void Awake()
        {
            if (_strike == null) _strike = GetComponentInParent<PlayerEnemyStrike>();
        }

        /// <summary>Animation Event : la hache/pioche touche → applique le dégât du swing en cours.</summary>
        public void AnimImpact()
        {
            if (!Resolve()) return;
            _strike.NotifyAnimationImpact();
        }

        /// <summary>Animation Event : fin du clip d'attaque → déverrouille l'enchaînement.</summary>
        public void AnimSwingEnd()
        {
            if (!Resolve()) return;
            _strike.NotifyAnimationSwingEnd();
        }

        private bool Resolve()
        {
            if (_strike != null) return true;
            _strike = GetComponentInParent<PlayerEnemyStrike>();
            if (_strike == null && !_warned)
            {
                _warned = true;
                SurvainLog.Warn(SurvainLog.Category.Gameplay,
                    "PlayerAttackAnimationRelay : aucun PlayerEnemyStrike trouvé dans les parents — " +
                    "les Animation Events d'attaque ne seront pas relayés.", this);
            }
            return _strike != null;
        }
    }
}
