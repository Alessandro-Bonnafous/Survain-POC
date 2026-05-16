using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Survain.Core;
using Survain.Data;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Caméra orbitale 3e personne : pitch/yaw via la souris (avec smoothing), zoom à la molette,
    /// SphereCast anti-clipping. Suit le joueur. Verrouillage de la rotation exposé via API
    /// (préparation Sprint 2 — mode construction).
    ///
    /// Ce composant LIT les actions "Look" et "Zoom" mais ne gère PAS l'activation de la map "Player" :
    /// c'est PlayerController qui en est propriétaire (cf. son commentaire d'en-tête).
    ///
    /// Doit être placé sur le GameObject de la caméra (ou un parent direct).
    /// Verrouille le curseur dans la fenêtre au OnEnable et le libère au OnDisable.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        // ─── Configuration ──────────────────────────────────────────────────

        [Header("Configuration")]
        [FormerlySerializedAs("config")]
        [SerializeField] private PlayerCameraConfig _config;

        [Tooltip("Asset Input System partagé avec PlayerController. La map 'Player' doit y exposer 'Look' et 'Zoom'.")]
        [FormerlySerializedAs("inputActions")]
        [SerializeField] private InputActionAsset _inputActions;

        [Tooltip("Cible suivie (typiquement le Transform du joueur).")]
        [FormerlySerializedAs("target")]
        [SerializeField] private Transform _target;

        [Header("Collision")]
        [Tooltip("Layers contre lesquels la caméra recule pour éviter le clipping.")]
        [FormerlySerializedAs("collisionMask")]
        [SerializeField] private LayerMask _collisionMask = ~0; // tout par défaut

        // ─── Constantes ─────────────────────────────────────────────────────

        private const string ActionMapName = "Player";
        private const string LookActionName = "Look";
        private const string ZoomActionName = "Zoom";

        // ─── État runtime ───────────────────────────────────────────────────

        private InputAction _lookAction;
        private InputAction _zoomAction;

        // Rotation : on update une cible (entrée souris), la valeur courante rattrape via SmoothDamp.
        private float _yawDeg;
        private float _yawTarget;
        private float _yawVelocity;
        private float _pitchDeg = 15f;
        private float _pitchTarget = 15f;
        private float _pitchVelocity;

        // Zoom : pareil, target modifiée par la molette, current smoothée.
        private float _currentDistance;
        private float _targetDistance;
        private float _distanceVelocity;

        // ─── API publique ───────────────────────────────────────────────────

        /// <summary>
        /// Verrouille la rotation de la caméra (yaw/pitch figés à leur valeur courante).
        /// Le zoom continue de fonctionner. Réservé au futur mode construction (Sprint 2).
        /// </summary>
        public bool RotationLocked { get; set; }

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_config == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerCameraRig : config non assignée.", this);
                enabled = false;
                return;
            }

            if (_inputActions == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerCameraRig : inputActions non assigné.", this);
                enabled = false;
                return;
            }

            if (_target == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerCameraRig : target non assignée.", this);
                enabled = false;
                return;
            }

            var map = _inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            _lookAction = map?.FindAction(LookActionName, throwIfNotFound: false);
            _zoomAction = map?.FindAction(ZoomActionName, throwIfNotFound: false);
            if (_lookAction == null || _zoomAction == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    $"PlayerCameraRig : actions '{LookActionName}'/'{ZoomActionName}' introuvables dans la map '{ActionMapName}'.", this);
                enabled = false;
                return;
            }

            // Initialise rotation et distance sur des valeurs cohérentes pour éviter un snap au démarrage.
            Vector3 e = transform.eulerAngles;
            _yawDeg = e.y;
            _yawTarget = _yawDeg;
            _pitchDeg = Mathf.Clamp(NormalizePitch(e.x), _config.MinPitchDeg, _config.MaxPitchDeg);
            _pitchTarget = _pitchDeg;

            _currentDistance = Mathf.Clamp(_config.Distance, _config.MinDistance, _config.MaxDistance);
            _targetDistance = _currentDistance;
        }

        private void OnEnable()
        {
            if (_zoomAction != null) _zoomAction.performed += OnZoomPerformed;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            if (_zoomAction != null) _zoomAction.performed -= OnZoomPerformed;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ─── Input handlers ─────────────────────────────────────────────────

        private void OnZoomPerformed(InputAction.CallbackContext ctx)
        {
            // Scroll up (delta positif) = zoom in (distance diminue). Inversion par signe.
            float delta = ctx.ReadValue<float>();
            _targetDistance = Mathf.Clamp(
                _targetDistance - delta * _config.ZoomSensitivity,
                _config.MinDistance, _config.MaxDistance);
        }

        // ─── LateUpdate ─────────────────────────────────────────────────────

        private void LateUpdate()
        {
            // Souris : on update les cibles sauf si la rotation est verrouillée.
            // Mouse delta : NON multiplié par Time.deltaTime — la valeur est déjà un delta par frame.
            if (!RotationLocked)
            {
                Vector2 look = _lookAction.ReadValue<Vector2>();
                _yawTarget += look.x * _config.SensitivityX;
                float pitchDelta = look.y * _config.SensitivityY * (_config.InvertY ? 1f : -1f);
                _pitchTarget = Mathf.Clamp(_pitchTarget + pitchDelta, _config.MinPitchDeg, _config.MaxPitchDeg);
            }

            // Smoothing rotation : SmoothDampAngle gère le wrap-around 360° du yaw.
            _yawDeg = Mathf.SmoothDampAngle(_yawDeg, _yawTarget, ref _yawVelocity, _config.RotationSmoothTime);
            _pitchDeg = Mathf.SmoothDamp(_pitchDeg, _pitchTarget, ref _pitchVelocity, _config.RotationSmoothTime);

            // Smoothing zoom.
            _currentDistance = Mathf.SmoothDamp(_currentDistance, _targetDistance, ref _distanceVelocity, _config.ZoomSmoothTime);

            // Pose finale.
            Quaternion rot = Quaternion.Euler(_pitchDeg, _yawDeg, 0f);
            Vector3 pivot = _target.position + Vector3.up * _config.PivotHeightOffset;
            Vector3 dirFromPivot = rot * Vector3.back;

            // SphereCast pour reculer la caméra si le décor est entre le pivot et la position désirée.
            float resolved = _currentDistance;
            if (Physics.SphereCast(pivot, _config.CollisionRadius, dirFromPivot,
                    out RaycastHit hit, _currentDistance, _collisionMask, QueryTriggerInteraction.Ignore))
            {
                resolved = Mathf.Max(0f, hit.distance - _config.CollisionPadding);
            }

            transform.SetPositionAndRotation(pivot + dirFromPivot * resolved, rot);
        }

        // ─── Utilitaires ────────────────────────────────────────────────────

        private static float NormalizePitch(float eulerX)
        {
            // EulerAngles renvoie [0..360]. On veut [-180..180] pour le clamp.
            return eulerX > 180f ? eulerX - 360f : eulerX;
        }
    }
}
