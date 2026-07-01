using UnityEngine;
using Survain.Core;
using Survain.Items;

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
    ///  - isDodging     (trigger) : déclenché au frame exact où une esquive démarre (anim de roulade)
    ///  - isHarvesting  (trigger) : déclenché à chaque coup qui touche un nœud (récolte) ou un ennemi (arme)
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

        [Tooltip("Frappe ennemie (placeholder combat). Auto-récupérée sur le même GameObject si non assignée. Optionnel : un coup d'arme rejoue l'anim de l'outil (Chop/Mine).")]
        [SerializeField] private PlayerEnemyStrike _strike;

        [Tooltip("Équipement joueur. Auto-récupéré sur le même GameObject si non assigné. Optionnel : renseigne harvestType pour choisir l'anim de récolte selon le type d'outil (hache/pioche/...).")]
        [SerializeField] private PlayerEquipment _equipment;

        private static readonly int SpeedHash = Animator.StringToHash("speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("isGrounded");
        private static readonly int IsJumpingHash = Animator.StringToHash("isJumping");
        private static readonly int IsDodgingHash = Animator.StringToHash("isDodging");
        private static readonly int IsHarvestingHash = Animator.StringToHash("isHarvesting");
        private static readonly int HarvestTypeHash = Animator.StringToHash("harvestType");

        private void Awake()
        {
            if (_playerController == null) _playerController = GetComponent<PlayerController>();
            if (_characterController == null) _characterController = GetComponent<CharacterController>();
            if (_harvester == null) _harvester = GetComponent<PlayerHarvester>();
            if (_strike == null) _strike = GetComponent<PlayerEnemyStrike>();
            if (_equipment == null) _equipment = GetComponentInChildren<PlayerEquipment>();

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

            // Optionnel mais sans lui harvestType reste à 0 → aucune anim de récolte ne se déclenche.
            // On prévient explicitement plutôt que de dégrader en silence.
            if (_equipment == null)
                SurvainLog.Warn(SurvainLog.Category.Gameplay,
                    "PlayerVisualAnimator : PlayerEquipment introuvable — l'anim de récolte ne sera pas " +
                    "routée selon le type d'outil (harvestType reste à 0).", this);
        }

        private void OnEnable()
        {
            _playerController.Jumped += OnJumped;
            _playerController.Dodged += OnDodged;
            if (_harvester != null) _harvester.HitLanded += OnHitLanded;
            if (_strike != null) _strike.Swung += OnHitLanded; // coup d'arme → même anim que la récolte (selon harvestType)
            if (_equipment != null)
            {
                _equipment.OnCurrentToolChanged += OnEquippedToolChanged;
                SetHarvestType(_equipment.CurrentTool);
            }
        }

        private void OnDisable()
        {
            _playerController.Jumped -= OnJumped;
            _playerController.Dodged -= OnDodged;
            if (_harvester != null) _harvester.HitLanded -= OnHitLanded;
            if (_strike != null) _strike.Swung -= OnHitLanded;
            if (_equipment != null) _equipment.OnCurrentToolChanged -= OnEquippedToolChanged;
        }

        private void Update()
        {
            // Vitesse horizontale réelle du CharacterController (prend en compte les collisions/rampes).
            Vector3 v = _characterController.velocity;
            v.y = 0f;
            _animator.SetFloat(SpeedHash, v.magnitude);
            _animator.SetBool(IsGroundedHash, _characterController.isGrounded);

            DebugDodgeTick(); // DEBUG (à retirer)
        }

        private void OnJumped()
        {
            _animator.SetTrigger(IsJumpingHash);
        }

        private void OnDodged()
        {
            _animator.SetTrigger(IsDodgingHash);
            _dbgUntil = Time.time + 2.5f; // DEBUG (à retirer) : fenêtre d'observation du state Dodge
            SurvainLog.Info(SurvainLog.Category.Gameplay, "[DodgeDbg] OnDodged -> SetTrigger(isDodging)", this);
        }

        // ─── DEBUG esquive (à retirer une fois le bug résolu) ───────────────
        private float _dbgUntil;
        private bool _dbgWasDodge;
        private float _dbgLastNt;

        private void DebugDodgeTick()
        {
            if (Time.time >= _dbgUntil) return;
            var st = _animator.GetCurrentAnimatorStateInfo(0);
            bool isDodge = st.IsName("Dodge");
            if (isDodge && !_dbgWasDodge)
                SurvainLog.Info(SurvainLog.Category.Gameplay, $"[DodgeDbg] ENTER Dodge nt={st.normalizedTime:0.00}", this);
            else if (!isDodge && _dbgWasDodge)
                SurvainLog.Info(SurvainLog.Category.Gameplay, "[DodgeDbg] EXIT Dodge -> autre state", this);
            else if (isDodge && st.normalizedTime + 0.05f < _dbgLastNt)
                SurvainLog.Info(SurvainLog.Category.Gameplay, $"[DodgeDbg] RE-LOOP Dodge (nt {_dbgLastNt:0.00} -> {st.normalizedTime:0.00})", this);
            _dbgWasDodge = isDodge;
            _dbgLastNt = st.normalizedTime;
        }

        private void OnHitLanded()
        {
            _animator.SetTrigger(IsHarvestingHash);
        }

        private void OnEquippedToolChanged(ToolData previous, ToolData current) => SetHarvestType(current);

        // Renseigne harvestType = valeur de ToolType (hache=1, pioche=2, ...), 0 si aucun outil.
        // L'Animator s'en sert pour router le trigger isHarvesting vers la bonne anim (Chop/Mine/...).
        private void SetHarvestType(ToolData tool)
        {
            _animator.SetInteger(HarvestTypeHash, tool != null ? (int)tool.ToolType : 0);
        }
    }
}
