using UnityEngine;
using UnityEngine.AI;
using Survain.Core;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Runtime d'un PNJ : consomme un NPCData, pilote un NavMeshAgent et une machine à états
    /// polymorphe (cf. INpcState). Phase 1 (#12) : locomotion Idle ⇄ Wander. Phase 2b : états
    /// Working/Eating/Sleeping/Fleeing + perception (NpcPerception) qui force la fuite.
    ///
    /// L'Animator (optionnel) reçoit le paramètre "speed" (vitesse de l'agent) pour la locomotion
    /// — on réutilise le NpcAvatar.controller sur les PNJ. Les états peuvent piloter d'autres
    /// paramètres via la propriété Animator (ex. WorkingState → "isWorking").
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class NpcController : MonoBehaviour
    {
        [Tooltip("Données du PNJ (vitesse, errance, fuite). Peut être injecté par le spawner.")]
        [SerializeField] private NPCData _data;

        [Tooltip("Animator du visuel (optionnel). Reçoit 'speed' (locomotion) et 'isWorking'.")]
        [SerializeField] private Animator _animator;

        public NPCData Data => _data;
        public NavMeshAgent Agent { get; private set; }

        /// <summary>Animator du visuel (peut être null). Exposé pour les états (anim par état).</summary>
        public Animator Animator => _animator;

        /// <summary>Perception du PNJ (peut être null = aucune détection de menace).</summary>
        public NpcPerception Perception { get; private set; }

        /// <summary>Besoins du PNJ (peut être null = pas de faim/moral piloté).</summary>
        public NpcNeeds Needs { get; private set; }

        /// <summary>Point d'origine (lieu de spawn), centre de l'errance.</summary>
        public Vector3 HomePosition { get; private set; }

        private INpcState _currentState;

        /// <summary>Injecte la data (appelé par le spawner avant Start).</summary>
        public void SetData(NPCData data) => _data = data;

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Perception = GetComponent<NpcPerception>(); // optionnel
            Needs = GetComponent<NpcNeeds>();           // optionnel
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
            // Interruptions globales prioritaires (les états n'ont pas à les tester eux-mêmes).
            // Ordre de priorité : fuite (survie) > désertion (terminal) > faim.
            if (Perception != null && Perception.HasThreat)
            {
                if (!(_currentState is FleeingState)) ChangeState(new FleeingState());
            }
            else if (Needs != null && Needs.IsDeserting)
            {
                if (!(_currentState is DesertingState)) ChangeState(new DesertingState());
            }
            else if (Needs != null && Needs.IsHungry && !(_currentState is EatingState)
                     && EatingState.TryFindSpot(transform.position, out _)) // n'interrompt que si un feu existe
            {
                ChangeState(new EatingState());
            }

            _currentState?.Tick(this);

            if (_animator != null && Agent != null)
            {
                _animator.SetFloat(NpcAnimParams.Speed, Agent.velocity.magnitude);
            }
        }

        /// <summary>Transition vers un nouvel état (Exit de l'ancien, Enter du nouveau).</summary>
        public void ChangeState(INpcState next)
        {
            _currentState?.Exit(this);
            _currentState = next;
            _currentState?.Enter(this);
        }

        // --- DEBUG (#12) : forcer un état en jeu pour valider anim + transitions, en attendant
        // que les besoins (#13), métiers (#14) et routines (#15) les déclenchent réellement.
        // Sélectionner un PNJ spawné en Play, puis clic droit sur le composant → menu.
        [ContextMenu("DEBUG/Forcer Working")] private void DebugWorking() => ChangeState(new WorkingState());
        [ContextMenu("DEBUG/Forcer Eating")] private void DebugEating() => ChangeState(new EatingState());
        [ContextMenu("DEBUG/Forcer Sleeping")] private void DebugSleeping() => ChangeState(new SleepingState());
        [ContextMenu("DEBUG/Forcer Idle")] private void DebugIdle() => ChangeState(new IdleState());
    }
}
