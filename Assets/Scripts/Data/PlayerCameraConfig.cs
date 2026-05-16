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
        [Tooltip("Distance entre le pivot et la caméra (mètres).")]
        [Range(1f, 15f)]
        [FormerlySerializedAs("distance")]
        [SerializeField] private float _distance = 5f;

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
        public float PivotHeightOffset => _pivotHeightOffset;
        public float SensitivityX => _sensitivityX;
        public float SensitivityY => _sensitivityY;
        public bool InvertY => _invertY;
        public float MinPitchDeg => _minPitchDeg;
        public float MaxPitchDeg => _maxPitchDeg;
        public float CollisionRadius => _collisionRadius;
        public float CollisionPadding => _collisionPadding;
    }
}
