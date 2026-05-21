using UnityEngine;
using UnityEngine.Serialization;

namespace Survain.Data
{
    /// <summary>
    /// Réglages de la caméra orbitale 3e personne. Conteneur de données pur — aucune logique ici.
    /// Consommé par PlayerCameraRig (Survain.Gameplay.Player).
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerCameraConfig",
        menuName = "Survain/Data/Player/Camera Config",
        order = 31)]
    public sealed class PlayerCameraConfig : ScriptableObject
    {
        [Header("Cadrage")]
        [Tooltip("Distance de départ entre le pivot et la caméra (mètres). Sera clampée entre Min/Max au démarrage.")]
        [Range(1f, 15f)]
        [FormerlySerializedAs("distance")]
        [SerializeField] private float _distance = 5f;

        [Tooltip("Distance minimale autorisée par le zoom (mètres).")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _minDistance = 2f;

        [Tooltip("Distance maximale autorisée par le zoom (mètres).")]
        [Range(2f, 20f)]
        [SerializeField] private float _maxDistance = 10f;

        [Tooltip("Hauteur du pivot au-dessus de la position du joueur (mètres). Typiquement la hauteur d'épaule.")]
        [Range(0f, 3f)]
        [FormerlySerializedAs("pivotHeightOffset")]
        [SerializeField] private float _pivotHeightOffset = 1.6f;

        [Header("Sensibilité souris")]
        [Tooltip("Sensibilité horizontale (degrés par pixel de delta).")]
        [Range(0.01f, 1f)]
        [FormerlySerializedAs("sensitivityX")]
        [SerializeField] private float _sensitivityX = 0.15f;

        [Tooltip("Sensibilité verticale (degrés par pixel de delta).")]
        [Range(0.01f, 1f)]
        [FormerlySerializedAs("sensitivityY")]
        [SerializeField] private float _sensitivityY = 0.15f;

        [Tooltip("Inverse l'axe vertical de la souris.")]
        [FormerlySerializedAs("invertY")]
        [SerializeField] private bool _invertY = false;

        [Header("Zoom")]
        [Tooltip("Sensibilité du zoom (mètres ajoutés/retirés par cran de molette).")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _zoomSensitivity = 1f;

        [Tooltip("Temps de smoothing du zoom (secondes). 0 = snap immédiat.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _zoomSmoothTime = 0.12f;

        [Header("Smoothing rotation")]
        [Tooltip("Temps de smoothing yaw/pitch (secondes). 0 = pas de smoothing (comportement direct).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _rotationSmoothTime = 0.08f;

        [Header("Feedback")]
        [Tooltip("Vitesse de retour du 'punch' caméra (rate exponentiel). Plus élevé = retour plus rapide.")]
        [Range(1f, 30f)]
        [SerializeField] private float _punchDecayRate = 8f;

        [Header("Limites verticales")]
        [Tooltip("Pitch minimum (degrés). Négatif = caméra regarde vers le bas.")]
        [Range(-89f, 0f)]
        [FormerlySerializedAs("minPitchDeg")]
        [SerializeField] private float _minPitchDeg = -30f;

        [Tooltip("Pitch maximum (degrés). Positif = caméra regarde vers le haut.")]
        [Range(0f, 89f)]
        [FormerlySerializedAs("maxPitchDeg")]
        [SerializeField] private float _maxPitchDeg = 70f;

        [Header("Collision")]
        [Tooltip("Rayon de la sphère de test de collision (mètres). Évite que la caméra traverse le décor.")]
        [Range(0.05f, 1f)]
        [FormerlySerializedAs("collisionRadius")]
        [SerializeField] private float _collisionRadius = 0.25f;

        [Tooltip("Marge ajoutée devant un point de collision pour éviter le clipping (mètres).")]
        [Range(0f, 1f)]
        [FormerlySerializedAs("collisionPadding")]
        [SerializeField] private float _collisionPadding = 0.1f;

        public float Distance => _distance;
        public float MinDistance => _minDistance;
        public float MaxDistance => _maxDistance;
        public float PivotHeightOffset => _pivotHeightOffset;
        public float SensitivityX => _sensitivityX;
        public float SensitivityY => _sensitivityY;
        public bool InvertY => _invertY;
        public float MinPitchDeg => _minPitchDeg;
        public float MaxPitchDeg => _maxPitchDeg;
        public float CollisionRadius => _collisionRadius;
        public float CollisionPadding => _collisionPadding;
        public float ZoomSensitivity => _zoomSensitivity;
        public float ZoomSmoothTime => _zoomSmoothTime;
        public float RotationSmoothTime => _rotationSmoothTime;
        public float PunchDecayRate => _punchDecayRate;
    }
}
