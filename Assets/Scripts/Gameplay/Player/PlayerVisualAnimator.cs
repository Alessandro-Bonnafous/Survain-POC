using UnityEngine;
using Survain.Core;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Pilote l'Animator d'un avatar humanoïde enfant du _Player.
    /// Lit la vitesse horizontale du CharacterController et l'état grounded chaque frame,
    /// et s'abonne à PlayerController.Jumped pour déclencher l'animation de saut.
    ///
    /// Paramètres Animator attendus :
    ///  - speed         (float)   : magnitude horizontale en m/s (alimente le Blend Tree de locomotion)
    ///  - isGrounded    (bool)    : true quand le CharacterController touche le sol
    ///  - isJumping     (trigger) : déclenché au frame exact du décollage
    ///  - isHarvesting  (trigger) : déclenché à chaque coup de récolte qui touche un nœud
    ///
    /// Convention : composant à poser sur le GameObject racine _Player. L'avatar (mesh + skin)
    /// est un enfant. La référence _animator pointe sur l'Animator de cet enfant.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerVisualAnimator : MonoBehaviour
    {
        [Tooltip("Animator de l'avatar enfant. Doit exposer speed (float), isGrounded (bool), isJumping (trigger), isHarvesting (trigger).")]
        [SerializeField] private Animator _animator;

        [Tooltip("Contrôleur joueur de la racine. Auto-récupéré sur le même GameObject si non assigné.")]
        [SerializeField] private PlayerController _playerController;

        [Tooltip("CharacterController de la racine. Auto-récupéré sur le même GameObject si non assigné.")]
        [SerializeField] private CharacterController _characterController;

        [Tooltip("Système de récolte. Auto-récupéré sur le même GameObject si non assigné. Optionnel : si absent, l'anim de récolte ne sera pas déclenchée.")]
        [SerializeField] private PlayerHarvester _harvester;

        private static readonly int SpeedHash = Animator.StringToHash("speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int IsJumpingHash = Animator.StringToHash("isJumping");
        private static readonly int IsHarvestingHash = Animator.StringToHash("isHarvesting");

        private void Awake()
        {
            if (_playerController == null) _playerController = GetComponent<PlayerController>();
            if (_characterController == null) _characterController = GetComponent<CharacterController>();
            if (_harvester == null) _harvester = GetComponent<PlayerHarvester>();

            if (_animator == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerVisualAnimator : animator non assigné.", this);
                enabled = false;
                return;
            }

            if (_playerController == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerVisualAnimator : PlayerController introuvable sur le même GameObject.", this);
                enabled = false;
                return;
            }

            if (_characterController == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerVisualAnimator : CharacterController introuvable sur le même GameObject.", this);
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            _playerController.Jumped += OnJumped;
            if (_harvester != null) _harvester.HitLanded += OnHitLanded;
        }

        private void OnDisable()
        {
            _playerController.Jumped -= OnJumped;
            if (_harvester != null) _harvester.HitLanded -= OnHitLanded;
        }

        private void Update()
        {
            // Vitesse horizontale réelle du CharacterController (prend en compte les collisions/rampes).
            Vector3 v = _characterController.velocity;
            v.y = 0f;
            _animator.SetFloat(SpeedHash, v.magnitude);
            _animator.SetBool(IsGroundedHash, _characterController.isGrounded);
        }

        private void OnJumped()
        {
            _animator.SetTrigger(IsJumpingHash);
        }

        private void OnHitLanded()
        {
            _animator.SetTrigger(IsHarvestingHash);
        }
    }
}
