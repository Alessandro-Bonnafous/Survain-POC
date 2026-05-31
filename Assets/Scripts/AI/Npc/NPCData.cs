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

        public string DisplayName => _displayName;
        public float MoveSpeed => _moveSpeed;
        public float WanderRadius => _wanderRadius;
        public float IdlePauseMin => _idlePauseMin;
        public float IdlePauseMax => Mathf.Max(_idlePauseMin, _idlePauseMax);
    }
}
