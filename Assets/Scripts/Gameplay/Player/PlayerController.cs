using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Survain.Core;
using Survain.Data;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Contrôleur 3e personne du joueur : déplacement caméra-relatif, saut, sprint, esquive, gravité.
    /// Anime un CharacterController. Le visuel (capsule placeholder) est un enfant du GameObject.
    ///
    /// Combat #16 (A3) : la course (sprint) draine l'énergie et l'esquive (action Dodge) consomme de
    /// l'énergie pour un dash bref + i-frames (via PlayerHealth.GrantInvulnerability). PlayerEnergy et
    /// PlayerHealth sont auto-résolus sur le même GameObject (optionnels : fallback permissif).
    ///
    /// Dépendances inspector requises : un PlayerMovementConfig, un InputActionAsset
    /// (la map "Player" doit exposer Move, Jump, Sprint ; Dodge est optionnelle), et un Transform de
    /// caméra pour orienter le mouvement (typiquement le rig caméra qui suit ce joueur).
    ///
    /// Convention : ce composant est l'unique propriétaire du cycle Enable/Disable de la map "Player".
    /// Les autres consommateurs (ex: PlayerCameraRig) lisent les actions sans toucher à l'activation.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
        /// <summary>Instance unique du joueur en scène (set OnEnable). Consommée par les systèmes
        /// qui ont besoin de cibler le joueur sans FindObjectOfType (ex. aggro des ennemis, #17).</summary>
        public static PlayerController Instance { get; private set; }

        // ─── Configuration ──────────────────────────────────────────────────

        [Header("Configuration")]
        [FormerlySerializedAs("config")]
        [SerializeField] private PlayerMovementConfig _config;

        [Tooltip("Asset Input System partagé. La map 'Player' sera activée au OnEnable.")]
        [FormerlySerializedAs("inputActions")]
        [SerializeField] private InputActionAsset _inputActions;

        [Tooltip("Transform de la caméra utilisée pour orienter le mouvement (ex: la Main Camera).")]
        [FormerlySerializedAs("cameraTransform")]
        [SerializeField] private Transform _cameraTransform;

        // ─── Constantes ─────────────────────────────────────────────────────

        private const string ActionMapName = "Player";
        private const string MoveActionName = "Move";
        private const string JumpActionName = "Jump";
        private const string SprintActionName = "Sprint";
        private const string DodgeActionName = "Dodge";

        // ─── Events ─────────────────────────────────────────────────────────

        /// <summary>
        /// Déclenché au frame exact du décollage (saut effectif, pas seulement l'input).
        /// Consommé par PlayerVisualAnimator pour déclencher l'anim de jump.
        /// </summary>
        public event Action Jumped;

        /// <summary>
        /// Déclenché au frame exact où une esquive démarre réellement (énergie OK, pas en plein dash).
        /// Consommé par PlayerVisualAnimator pour jouer l'anim de roulade. Même pattern que <see cref="Jumped"/>.
        /// </summary>
        public event Action Dodged;

        // ─── État runtime ───────────────────────────────────────────────────

        private CharacterController _characterController;
        private InputActionMap _playerMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _dodgeAction;

        // Énergie & vie (combat #16, A3) : auto-résolues (même GameObject _Player). Optionnelles
        // (fallback permissif si absentes).
        private PlayerEnergy _energy;
        private PlayerHealth _health;

        private Vector3 _velocity; // (x, z) = horizontal courant ; y = vertical (gravité/saut)
        private bool _jumpRequested;
        private bool _dodgeRequested;
        private float _dodgeTimeRemaining;
        private Vector3 _dodgeDir;

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_config == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerController : config non assignée.", this);
                enabled = false;
                return;
            }

            if (_inputActions == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerController : inputActions non assigné.", this);
                enabled = false;
                return;
            }

            if (_cameraTransform == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerController : cameraTransform non assigné.", this);
                enabled = false;
                return;
            }

            _playerMap = _inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            if (_playerMap == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    $"PlayerController : map '{ActionMapName}' introuvable dans {_inputActions.name}.", this);
                enabled = false;
                return;
            }

            _moveAction = _playerMap.FindAction(MoveActionName, throwIfNotFound: false);
            _jumpAction = _playerMap.FindAction(JumpActionName, throwIfNotFound: false);
            _sprintAction = _playerMap.FindAction(SprintActionName, throwIfNotFound: false);

            if (_moveAction == null || _jumpAction == null || _sprintAction == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerController : une ou plusieurs actions (Move/Jump/Sprint) introuvables dans la map 'Player'.", this);
                enabled = false;
                return;
            }

            // Dodge optionnel : son absence ne désactive pas la locomotion (esquive juste indisponible).
            _dodgeAction = _playerMap.FindAction(DodgeActionName, throwIfNotFound: false);
            if (_dodgeAction == null)
            {
                SurvainLog.Warn(SurvainLog.Category.Gameplay,
                    $"PlayerController : action '{DodgeActionName}' introuvable — esquive désactivée.", this);
            }

            // Énergie & vie : satellites sur le même _Player (A1 #81 / vie #19). Optionnels.
            _energy = GetComponent<PlayerEnergy>();
            _health = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            Instance = this;
            if (_jumpAction != null) _jumpAction.performed += OnJumpPerformed;
            if (_dodgeAction != null) _dodgeAction.performed += OnDodgePerformed;
            if (_playerMap != null) _playerMap.Enable();
        }

        private void OnDisable()
        {
            if (Instance == this) Instance = null;
            if (_jumpAction != null) _jumpAction.performed -= OnJumpPerformed;
            if (_dodgeAction != null) _dodgeAction.performed -= OnDodgePerformed;
            if (_playerMap != null) _playerMap.Disable();
        }

        // ─── API publique ───────────────────────────────────────────────────

        /// <summary>
        /// Téléporte le joueur (respawn, #19). Désactive temporairement le CharacterController
        /// pour forcer la position (le CC écrase sinon les écritures directes de transform),
        /// puis réinitialise la vitesse pour éviter une chute/glissade résiduelle.
        /// </summary>
        public void Teleport(Vector3 position)
        {
            if (_characterController != null) _characterController.enabled = false;
            transform.position = position;
            if (_characterController != null) _characterController.enabled = true;
            _velocity = Vector3.zero;
        }

        // ─── Input handlers ─────────────────────────────────────────────────

        private void OnJumpPerformed(InputAction.CallbackContext _)
        {
            // On bufferise la requête, traitée dans Update sous condition de grounded.
            _jumpRequested = true;
        }

        private void OnDodgePerformed(InputAction.CallbackContext _)
        {
            // Bufferisée, traitée dans Update (besoin de la direction de mouvement courante).
            _dodgeRequested = true;
        }

        // ─── Update ─────────────────────────────────────────────────────────

        private void Update()
        {
            float dt = Time.deltaTime;

            Vector2 moveInput = _moveAction.ReadValue<Vector2>();

            // Direction caméra-relative projetée sur le plan horizontal.
            Vector3 camForward = _cameraTransform.forward;
            Vector3 camRight = _cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 wishDir = camForward * moveInput.y + camRight * moveInput.x;
            float wishMag = Mathf.Min(wishDir.magnitude, 1f);
            if (wishMag > 0.0001f) wishDir /= wishDir.magnitude; else wishDir = Vector3.zero;
            bool moving = wishMag > 0.05f;

            // Course (A3) : draine l'énergie en continu tant qu'on sprinte en mouvement (hors esquive).
            // À sec → marche.
            bool sprinting = _dodgeTimeRemaining <= 0f && _sprintAction.IsPressed() && moving;
            if (sprinting && _energy != null && !_energy.TryConsume(_config.SprintEnergyPerSecond * dt))
                sprinting = false;

            // Esquive (A3) : dash bref + i-frames, coûte de l'énergie (spec : 40 %). Pas de relance en plein dash.
            if (_dodgeRequested)
            {
                _dodgeRequested = false;
                if (_dodgeTimeRemaining <= 0f)
                {
                    bool hasEnergy = _energy == null || _energy.TryConsume(_config.DodgeEnergyCost);
                    if (hasEnergy)
                    {
                        _dodgeDir = moving ? wishDir : transform.forward;
                        _dodgeTimeRemaining = _config.DodgeDurationSeconds;
                        if (_health != null) _health.GrantInvulnerability(_config.DodgeIFrameSeconds);
                        Dodged?.Invoke(); // déclenche l'anim de roulade (PlayerVisualAnimator)
                    }
                    else
                    {
                        SurvainLog.Info(SurvainLog.Category.Gameplay,
                            "Pas assez d'énergie pour esquiver.", this);
                    }
                }
            }

            // Vitesse horizontale : le dash d'esquive prime sur le déplacement normal.
            Vector3 horizontal;
            if (_dodgeTimeRemaining > 0f)
            {
                _dodgeTimeRemaining -= dt;
                horizontal = _dodgeDir * _config.DodgeSpeed;
            }
            else
            {
                float targetSpeed = _config.WalkSpeed * (sprinting ? _config.SprintMultiplier : 1f) * wishMag;
                horizontal = wishDir * targetSpeed;
            }

            // Rotation du joueur dans la direction visée (uniquement si on bouge).
            if (wishMag > 0.05f)
            {
                Quaternion targetRot = Quaternion.LookRotation(wishDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot,
                    _config.RotationSpeedDegPerSec * dt);
            }

            // Gravité / saut
            bool grounded = _characterController.isGrounded;
            if (grounded && _velocity.y < 0f)
            {
                _velocity.y = _config.GroundedStickForce;
            }

            if (_jumpRequested)
            {
                _jumpRequested = false;
                if (grounded)
                {
                    // v0 tel que h_max = v0² / (2g) → v0 = sqrt(2 * h * |g|)
                    _velocity.y = Mathf.Sqrt(2f * _config.JumpHeight * -_config.Gravity);
                    Jumped?.Invoke();
                }
            }

            _velocity.y += _config.Gravity * dt;

            Vector3 motion = (horizontal + Vector3.up * _velocity.y) * dt;
            _characterController.Move(motion);
        }
    }
}
