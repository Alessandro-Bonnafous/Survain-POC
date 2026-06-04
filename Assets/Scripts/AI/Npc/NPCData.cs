using UnityEngine;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Données d'un type de PNJ (ScriptableObject). Squelette du Sprint 3 (#12) : pour
    /// l'instant la locomotion (vitesse, errance). Le métier, les besoins (faim/moral/abri)
    /// et le moral viendront enrichir ce SO en #13/#14 — on garde la surface minimale ici.
    ///
    /// Même pattern que ItemData : SO data pur + MonoBehaviour runtime (NpcController) qui le
    /// consomme. Namespace Survain.AI.Npc (cf. conventions).
    /// </summary>
    [CreateAssetMenu(fileName = "NPCData", menuName = "Survain/AI/NPC Data", order = 80)]
    public sealed class NPCData : ScriptableObject
    {
        [Header("Identité")]
        [Tooltip("Nom affiché du PNJ.")]
        [SerializeField] private string _displayName = "Villageois";

        [Header("Locomotion")]
        [Tooltip("Vitesse de déplacement (NavMeshAgent).")]
        [Min(0.1f)]
        [SerializeField] private float _moveSpeed = 3.5f;

        [Tooltip("Rayon d'errance autour du point d'origine (mètres).")]
        [Min(1f)]
        [SerializeField] private float _wanderRadius = 8f;

        [Header("Pause (état Idle)")]
        [Tooltip("Durée minimale d'attente avant de repartir errer (secondes).")]
        [Min(0f)]
        [SerializeField] private float _idlePauseMin = 1.5f;

        [Tooltip("Durée maximale d'attente avant de repartir errer (secondes).")]
        [Min(0f)]
        [SerializeField] private float _idlePauseMax = 4f;

        [Header("Fuite (état Fleeing)")]
        [Tooltip("Vitesse de course pendant la fuite (NavMeshAgent). Caler le seuil Running du " +
                 "blend tree dessus pour que la fuite s'anime en course.")]
        [Min(0.1f)]
        [SerializeField] private float _fleeSpeed = 6f;

        [Header("Besoins — Faim (#13)")]
        [Tooltip("Décroissance de la faim par seconde (jauge 1 = rassasié).")]
        [Min(0f)]
        [SerializeField] private float _hungerDecayPerSecond = 0.015f;

        [Tooltip("Sous ce seuil de faim, le PNJ cherche à manger (phase 2).")]
        [Range(0f, 1f)]
        [SerializeField] private float _hungerSeekThreshold = 0.35f;

        [Tooltip("Restauration de la faim par seconde en mangeant (EatingState, phase 2).")]
        [Min(0f)]
        [SerializeField] private float _eatRatePerSecond = 0.25f;

        [Header("Besoins — Moral (#13)")]
        [Tooltip("Poids de la faim dans le calcul du moral cible.")]
        [Min(0f)]
        [SerializeField] private float _moraleHungerWeight = 1f;

        [Tooltip("Poids de l'abri dans le calcul du moral cible.")]
        [Min(0f)]
        [SerializeField] private float _moraleShelterWeight = 0.5f;

        [Tooltip("Vitesse de convergence du moral vers sa cible (par seconde).")]
        [Min(0.01f)]
        [SerializeField] private float _moraleLerpSpeed = 0.2f;

        [Tooltip("Sous ce moral, le PNJ déserte le village (phase 2).")]
        [Range(0f, 1f)]
        [SerializeField] private float _moraleDesertionThreshold = 0.05f;

        [Tooltip("Sous ce moral, une bulle d'alerte s'affiche au-dessus du PNJ (UI phase 3).")]
        [Range(0f, 1f)]
        [SerializeField] private float _moraleWarnThreshold = 0.3f;

        [Header("Besoins — Productivité (#13)")]
        [Tooltip("Multiplicateur de vitesse de travail au moral 0 (lent / grève).")]
        [Min(0f)]
        [SerializeField] private float _workSpeedAtZeroMorale = 0.25f;

        [Tooltip("Multiplicateur de vitesse de travail au moral maximal.")]
        [Min(0f)]
        [SerializeField] private float _workSpeedAtFullMorale = 1.2f;

        public string DisplayName => _displayName;
        public float MoveSpeed => _moveSpeed;
        public float WanderRadius => _wanderRadius;
        public float IdlePauseMin => _idlePauseMin;
        public float IdlePauseMax => Mathf.Max(_idlePauseMin, _idlePauseMax);
        public float FleeSpeed => _fleeSpeed;

        public float HungerDecayPerSecond => _hungerDecayPerSecond;
        public float HungerSeekThreshold => _hungerSeekThreshold;
        public float EatRatePerSecond => _eatRatePerSecond;
        public float MoraleHungerWeight => _moraleHungerWeight;
        public float MoraleShelterWeight => _moraleShelterWeight;
        public float MoraleLerpSpeed => _moraleLerpSpeed;
        public float MoraleDesertionThreshold => _moraleDesertionThreshold;
        public float MoraleWarnThreshold => _moraleWarnThreshold;
        public float WorkSpeedAtZeroMorale => _workSpeedAtZeroMorale;
        public float WorkSpeedAtFullMorale => _workSpeedAtFullMorale;
    }
}
