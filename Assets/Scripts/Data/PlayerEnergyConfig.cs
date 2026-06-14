using UnityEngine;

namespace Survain.Data
{
    /// <summary>
    /// Stats d'énergie du joueur (combat #16, Phase A / A1). Conteneur de données pur —
    /// aucune logique ici. Consommé par PlayerEnergy (Survain.Gameplay.Player). Même pattern
    /// que PlayerHealthConfig (un SO = un domaine cohérent).
    ///
    /// ⚠️ Valeurs de régénération = placeholders, réglées au pass d'équilibrage (#88).
    /// </summary>
    [CreateAssetMenu(
        fileName = "PlayerEnergyConfig",
        menuName = "Survain/Data/Player/Energy Config",
        order = 32)]
    public sealed class PlayerEnergyConfig : ScriptableObject
    {
        [Header("Énergie")]
        [Tooltip("Réserve d'énergie maximum du joueur (spec : 100).")]
        [Min(1f)]
        [SerializeField] private float _maxEnergy = 100f;

        [Header("Régénération")]
        [Tooltip("Énergie régénérée par seconde après un délai sans consommation. "
            + "Placeholder (équilibrage #88). 0 = pas de régén.")]
        [Min(0f)]
        [SerializeField] private float _regenPerSecond = 15f;

        [Tooltip("Délai sans consommer d'énergie avant que la régénération ne reprenne (secondes). "
            + "Placeholder (équilibrage #88).")]
        [Min(0f)]
        [SerializeField] private float _regenDelaySeconds = 1f;

        public float MaxEnergy => _maxEnergy;
        public float RegenPerSecond => _regenPerSecond;
        public float RegenDelaySeconds => _regenDelaySeconds;
    }
}
