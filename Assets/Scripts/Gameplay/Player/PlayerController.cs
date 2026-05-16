using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Survain.Core;
using Survain.Data;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Contrôleur 3e personne du joueur : déplacement caméra-relatif, saut, sprint, gravité.
    /// Anime un CharacterController. Le visuel (capsule placeholder) est un enfant du GameObject.
    ///
    /// Dépendances inspector requises : un PlayerMovementConfig, un InputActionAsset
    /// (la map "Player" doit exposer les actions Move, Jump, Sprint), et un Transform de caméra
    /// pour orienter le mouvement (typiquement le rig caméra qui suit ce joueur).
    ///
    /// Convention : ce composant est l'unique propriétaire du cycle Enable/Disable de la map "Player".
    /// Les autres consommateurs (ex: PlayerCameraRig) lisent les actions sans toucher à l'activation.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [DisallowMultipleComponent]
    public sealed class PlayerController : MonoBehaviour
    {
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

        // ─── État runtime ───────────────────────────────────────────────────

        private CharacterController _characterController;
        private InputActionMap _playerMap;
        private InputAction _moveAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;

        private Vector3 _velocity; // (x, z) = horizontal courant ; y = vertical (gravité/saut)
        private bool _jumpRequested;

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
        }

        private void OnEnable()
        {
            if (_jumpAction != null) _jumpAction.performed += OnJumpPerformed;
            if (_playerMap != null) _playerMap.Enable();
        }

        private void OnDisable()
        {
            if (_jumpAction != null) _jumpAction.performed -= OnJumpPerformed;
            if (_playerMap != null) _playerMap.Disable();
        }

        // ─── Input handlers ─────────────────────────────────────────────────

        private void OnJumpPerformed(InputAction.CallbackContext _)
        {
            // On bufferise la requête, traitée dans Update sous condition de grounded.
            _jumpRequested = true;
        }

        // ─── Update ─────────────────────────────────────────────────────────

        private void Update()
        {
            float dt = Time.deltaTime;

            Vector2 moveInput = _moveAction.ReadValue<Vector2>();
            bool sprinting = _sprintAction.IsPressed();

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

            float targetSpeed = _config.WalkSpeed * (sprinting ? _config.SprintMultiplier : 1f) * wishMag;
            Vector3 horizontal = wishDir * targetSpeed;

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
                }
            }

            _velocity.y += _config.Gravity * dt;

            Vector3 motion = (horizontal + Vector3.up * _velocity.y) * dt;
            _characterController.Move(motion);
        }
    }
}
