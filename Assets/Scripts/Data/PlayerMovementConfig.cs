using UnityEngine;

namespace Survain.Data
{
    /// <summary>
    /// Stats de locomotion du joueur. Conteneur de données pur — aucune logique ici.
    /// Consommé par PlayerController (Survain.Gameplay.Player).
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerMovementConfig",
        menuName = "Survain/Data/Player/Movement Config",
        order = 30)]
    public sealed class PlayerMovementConfig : ScriptableObject
    {
        [Header("Vitesses")]
        [Tooltip("Vitesse de marche (m/s).")]
        [Range(1f, 15f)]
        [SerializeField] private float walkSpeed = 5f;

        [Tooltip("Multiplicateur de vitesse en sprint.")]
        [Range(1f, 3f)]
        [SerializeField] private float sprintMultiplier = 1.6f;

        [Header("Saut & gravité")]
        [Tooltip("Hauteur de saut (mètres).")]
        [Range(0.5f, 4f)]
        [SerializeField] private float jumpHeight = 1.4f;

        [Tooltip("Gravité appliquée au joueur (m/s², négatif). Plus arcade que -9.81 pour un POC réactif.")]
        [Range(-40f, -1f)]
        [SerializeField] private float gravity = -20f;

        [Tooltip("Force verticale appliquée quand on est au sol pour rester collé (m/s, négatif).")]
        [Range(-10f, 0f)]
        [SerializeField] private float groundedStickForce = -2f;

        [Header("Rotation")]
        [Tooltip("Vitesse de rotation du joueur vers la direction du mouvement (degrés/sec).")]
        [Range(90f, 1440f)]
        [SerializeField] private float rotationSpeedDegPerSec = 720f;

        public float WalkSpeed => walkSpeed;
        public float SprintMultiplier => sprintMultiplier;
        public float JumpHeight => jumpHeight;
        public float Gravity => gravity;
        public float GroundedStickForce => groundedStickForce;
        public float RotationSpeedDegPerSec => rotationSpeedDegPerSec;
    }
}
