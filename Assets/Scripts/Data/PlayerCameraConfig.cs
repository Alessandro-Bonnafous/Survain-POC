using UnityEngine;

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
        [SerializeField] private float distance = 5f;

        [Tooltip("Hauteur du pivot au-dessus de la position du joueur (mètres). Typiquement la hauteur d'épaule.")]
        [Range(0f, 3f)]
        [SerializeField] private float pivotHeightOffset = 1.6f;

        [Header("Sensibilité souris")]
        [Tooltip("Sensibilité horizontale (degrés par pixel de delta).")]
        [Range(0.01f, 1f)]
        [SerializeField] private float sensitivityX = 0.15f;

        [Tooltip("Sensibilité verticale (degrés par pixel de delta).")]
        [Range(0.01f, 1f)]
        [SerializeField] private float sensitivityY = 0.15f;

        [Tooltip("Inverse l'axe vertical de la souris.")]
        [SerializeField] private bool invertY = false;

        [Header("Limites verticales")]
        [Tooltip("Pitch minimum (degrés). Négatif = caméra regarde vers le bas.")]
        [Range(-89f, 0f)]
        [SerializeField] private float minPitchDeg = -30f;

        [Tooltip("Pitch maximum (degrés). Positif = caméra regarde vers le haut.")]
        [Range(0f, 89f)]
        [SerializeField] private float maxPitchDeg = 70f;

        [Header("Collision")]
        [Tooltip("Rayon de la sphère de test de collision (mètres). Évite que la caméra traverse le décor.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float collisionRadius = 0.25f;

        [Tooltip("Marge ajoutée devant un point de collision pour éviter le clipping (mètres).")]
        [Range(0f, 1f)]
        [SerializeField] private float collisionPadding = 0.1f;

        public float Distance => distance;
        public float PivotHeightOffset => pivotHeightOffset;
        public float SensitivityX => sensitivityX;
        public float SensitivityY => sensitivityY;
        public bool InvertY => invertY;
        public float MinPitchDeg => minPitchDeg;
        public float MaxPitchDeg => maxPitchDeg;
        public float CollisionRadius => collisionRadius;
        public float CollisionPadding => collisionPadding;
    }
}
