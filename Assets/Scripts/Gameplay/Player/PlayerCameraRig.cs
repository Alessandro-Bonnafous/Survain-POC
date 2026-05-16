using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Survain.Core;
using Survain.Data;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Caméra orbitale 3e personne : pitch/yaw via la souris, suit le joueur, recule contre le décor
    /// via un SphereCast pour éviter le clipping.
    ///
    /// Ce composant LIT l'action "Look" mais ne gère PAS l'activation de la map "Player" :
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

        [Tooltip("Asset Input System partagé avec PlayerController. La map 'Player' doit y exposer 'Look'.")]
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

        // ─── État runtime ───────────────────────────────────────────────────

        private InputAction _lookAction;
        private float _yawDeg;
        private float _pitchDeg = 15f;

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
            if (_lookAction == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    $"PlayerCameraRig : action '{LookActionName}' introuvable dans la map '{ActionMapName}'.", this);
                enabled = false;
                return;
            }

            // Initialise yaw sur l'orientation actuelle de la caméra pour éviter un snap au démarrage.
            Vector3 e = transform.eulerAngles;
            _yawDeg = e.y;
            _pitchDeg = NormalizePitch(e.x);
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ─── LateUpdate ─────────────────────────────────────────────────────

        private void LateUpdate()
        {
            // Mouse delta : NON multiplié par Time.deltaTime — la valeur est déjà un delta par frame.
            Vector2 look = _lookAction.ReadValue<Vector2>();

            _yawDeg += look.x * _config.SensitivityX;
            float pitchDelta = look.y * _config.SensitivityY * (_config.InvertY ? 1f : -1f);
            _pitchDeg = Mathf.Clamp(_pitchDeg + pitchDelta, _config.MinPitchDeg, _config.MaxPitchDeg);

            Quaternion rot = Quaternion.Euler(_pitchDeg, _yawDeg, 0f);

            Vector3 pivot = _target.position + Vector3.up * _config.PivotHeightOffset;
            Vector3 dirFromPivot = rot * Vector3.back;

            // SphereCast pour reculer la caméra si le décor est entre le pivot et la position désirée.
            float maxDistance = _config.Distance;
            float resolved = maxDistance;
            if (Physics.SphereCast(pivot, _config.CollisionRadius, dirFromPivot,
                    out RaycastHit hit, maxDistance, _collisionMask, QueryTriggerInteraction.Ignore))
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
