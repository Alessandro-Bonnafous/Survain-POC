using UnityEngine;

namespace Survain.Data
{
    /// <summary>
    /// Stats de vie du joueur (#19). Conteneur de données pur — aucune logique ici.
    /// Consommé par PlayerHealth (Survain.Gameplay.Player). Même pattern que
    /// PlayerMovementConfig / PlayerCameraConfig (un SO = un domaine cohérent).
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerHealthConfig",
        menuName = "Survain/Data/Player/Health Config",
        order = 31)]
    public sealed class PlayerHealthConfig : ScriptableObject
    {
        [Header("Vie")]
        [Tooltip("Points de vie maximum du joueur.")]
        [Min(1)]
        [SerializeField] private int _maxHp = 100;

        [Header("Régénération")]
        [Tooltip("PV régénérés par seconde après un délai sans dégât. 0 = pas de régén.")]
        [Min(0f)]
        [SerializeField] private float _regenPerSecond = 4f;

        [Tooltip("Délai sans subir de dégât avant que la régénération ne reprenne (secondes).")]
        [Min(0f)]
        [SerializeField] private float _regenDelaySeconds = 6f;

        [Header("Encaissement")]
        [Tooltip("Fenêtre d'invulnérabilité après un coup (secondes). Évite l'enchaînement de "
            + "plusieurs sources sur la même frappe. 0 = aucune.")]
        [Min(0f)]
        [SerializeField] private float _invulnerabilitySeconds = 0.3f;

        public int MaxHp => _maxHp;
        public float RegenPerSecond => _regenPerSecond;
        public float RegenDelaySeconds => _regenDelaySeconds;
        public float InvulnerabilitySeconds => _invulnerabilitySeconds;
    }
}
