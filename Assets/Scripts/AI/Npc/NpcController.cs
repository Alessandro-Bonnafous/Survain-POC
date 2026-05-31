using UnityEngine;
using UnityEngine.AI;
using Survain.Core;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Runtime d'un PNJ : consomme un NPCData, pilote un NavMeshAgent et une machine à états
    /// polymorphe (cf. INpcState). Phase 1 (#12) : locomotion Idle ⇄ Wander.
    ///
    /// L'Animator (optionnel) reçoit le paramètre "speed" (vitesse de l'agent) pour jouer la
    /// locomotion — on réutilise le PlayerAvatar.controller du joueur (#33) sur les PNJ.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NpcController : MonoBehaviour
    {
        [Tooltip("Données du PNJ (vitesse, errance). Peut être injecté par le spawner.")]
        [SerializeField] private NPCData _data;

        [Tooltip("Animator du visuel (optionnel). Reçoit le paramètre 'speed'.")]
        [SerializeField] private Animator _animator;

        public NPCData Data => _data;
        public NavMeshAgent Agent { get; private set; }

        /// <summary>Point d'origine (lieu de spawn), centre de l'errance.</summary>
        public Vector3 HomePosition { get; private set; }

        private INpcState _currentState;
        private static readonly int SpeedHash = Animator.StringToHash("speed");

        /// <summary>Injecte la data (appelé par le spawner avant Start).</summary>
        public void SetData(NPCData data) => _data = data;

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            if (_data == null)
            {
                SurvainLog.Error(SurvainLog.Category.AI, "NpcController : NPCData manquant.", this);
                enabled = false;
                return;
            }

            Agent.speed = _data.MoveSpeed;
            HomePosition = transform.position; // après positionnement par le spawner
            ChangeState(new IdleState());
        }

        private void Update()
        {
            _currentState?.Tick(this);
            if (_animator != null && Agent != null)
            {
                _animator.SetFloat(SpeedHash, Agent.velocity.magnitude);
            }
        }

        /// <summary>Transition vers un nouvel état (Exit de l'ancien, Enter du nouveau).</summary>
        public void ChangeState(INpcState next)
        {
            _currentState?.Exit(this);
            _currentState = next;
            _currentState?.Enter(this);
        }
    }
}
