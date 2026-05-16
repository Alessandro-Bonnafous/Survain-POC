using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("walkSpeed")]
        [SerializeField] private float _walkSpeed = 5f;

        [Tooltip("Multiplicateur de vitesse en sprint.")]
        [Range(1f, 3f)]
        [FormerlySerializedAs("sprintMultiplier")]
        [SerializeField] private float _sprintMultiplier = 1.6f;

        [Header("Saut & gravité")]
        [Tooltip("Hauteur de saut (mètres).")]
        [Range(0.5f, 4f)]
        [FormerlySerializedAs("jumpHeight")]
        [SerializeField] private float _jumpHeight = 1.4f;

        [Tooltip("Gravité appliquée au joueur (m/s², négatif). Plus arcade que -9.81 pour un POC réactif.")]
        [Range(-40f, -1f)]
        [FormerlySerializedAs("gravity")]
        [SerializeField] private float _gravity = -20f;

        [Tooltip("Force verticale appliquée quand on est au sol pour rester collé (m/s, négatif).")]
        [Range(-10f, 0f)]
        [FormerlySerializedAs("groundedStickForce")]
        [SerializeField] private float _groundedStickForce = -2f;

        [Header("Rotation")]
        [Tooltip("Vitesse de rotation du joueur vers la direction du mouvement (degrés/sec).")]
        [Range(90f, 1440f)]
        [FormerlySerializedAs("rotationSpeedDegPerSec")]
        [SerializeField] private float _rotationSpeedDegPerSec = 720f;

        public float WalkSpeed => _walkSpeed;
        public float SprintMultiplier => _sprintMultiplier;
        public float JumpHeight => _jumpHeight;
        public float Gravity => _gravity;
        public float GroundedStickForce => _groundedStickForce;
        public float RotationSpeedDegPerSec => _rotationSpeedDegPerSec;
    }
}
