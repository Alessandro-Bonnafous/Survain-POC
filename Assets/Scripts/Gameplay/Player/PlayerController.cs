using UnityEngine;
using UnityEngine.InputSystem;
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
        [SerializeField] private PlayerMovementConfig config;

        [Tooltip("Asset Input System partagé. La map 'Player' sera activée au OnEnable.")]
        [SerializeField] private InputActionAsset inputActions;

        [Tooltip("Transform de la caméra utilisée pour orienter le mouvement (ex: la Main Camera).")]
        [SerializeField] private Transform cameraTransform;

        // ─── Constantes ─────────────────────────────────────────────────────

        private const string ActionMapName = "Player";
        private const string MoveActionName = "Move";
        private const string JumpActionName = "Jump";
        private const string SprintActionName = "Sprint";

        // ─── État runtime ───────────────────────────────────────────────────

        private CharacterController characterController;
        private InputActionMap playerMap;
        private InputAction moveAction;
        private InputAction jumpAction;
        private InputAction sprintAction;

        private Vector3 velocity; // (x, z) = horizontal courant ; y = vertical (gravité/saut)
        private bool jumpRequested;

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (config == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerController : config non assignée.", this);
                enabled = false;
                return;
            }

            if (inputActions == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerController : inputActions non assigné.", this);
                enabled = false;
                return;
            }

            if (cameraTransform == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerController : cameraTransform non assigné.", this);
                enabled = false;
                return;
            }

            playerMap = inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            if (playerMap == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    $"PlayerController : map '{ActionMapName}' introuvable dans {inputActions.name}.", this);
                enabled = false;
                return;
            }

            moveAction = playerMap.FindAction(MoveActionName, throwIfNotFound: false);
            jumpAction = playerMap.FindAction(JumpActionName, throwIfNotFound: false);
            sprintAction = playerMap.FindAction(SprintActionName, throwIfNotFound: false);

            if (moveAction == null || jumpAction == null || sprintAction == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerController : une ou plusieurs actions (Move/Jump/Sprint) introuvables dans la map 'Player'.", this);
                enabled = false;
                return;
            }
        }

        private void OnEnable()
        {
            if (jumpAction != null) jumpAction.performed += OnJumpPerformed;
            if (playerMap != null) playerMap.Enable();
        }

        private void OnDisable()
        {
            if (jumpAction != null) jumpAction.performed -= OnJumpPerformed;
            if (playerMap != null) playerMap.Disable();
        }

        // ─── Input handlers ─────────────────────────────────────────────────

        private void OnJumpPerformed(InputAction.CallbackContext _)
        {
            // On bufferise la requête, traitée dans Update sous condition de grounded.
            jumpRequested = true;
        }

        // ─── Update ─────────────────────────────────────────────────────────

        private void Update()
        {
            float dt = Time.deltaTime;

            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            bool sprinting = sprintAction.IsPressed();

            // Direction caméra-relative projetée sur le plan horizontal.
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 wishDir = camForward * moveInput.y + camRight * moveInput.x;
            float wishMag = Mathf.Min(wishDir.magnitude, 1f);
            if (wishMag > 0.0001f) wishDir /= wishDir.magnitude; else wishDir = Vector3.zero;

            float targetSpeed = config.WalkSpeed * (sprinting ? config.SprintMultiplier : 1f) * wishMag;
            Vector3 horizontal = wishDir * targetSpeed;

            // Rotation du joueur dans la direction visée (uniquement si on bouge).
            if (wishMag > 0.05f)
            {
                Quaternion targetRot = Quaternion.LookRotation(wishDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot,
                    config.RotationSpeedDegPerSec * dt);
            }

            // Gravité / saut
            bool grounded = characterController.isGrounded;
            if (grounded && velocity.y < 0f)
            {
                velocity.y = config.GroundedStickForce;
            }

            if (jumpRequested)
            {
                jumpRequested = false;
                if (grounded)
                {
                    // v0 tel que h_max = v0² / (2g) → v0 = sqrt(2 * h * |g|)
                    velocity.y = Mathf.Sqrt(2f * config.JumpHeight * -config.Gravity);
                }
            }

            velocity.y += config.Gravity * dt;

            Vector3 motion = (horizontal + Vector3.up * velocity.y) * dt;
            characterController.Move(motion);
        }
    }
}
