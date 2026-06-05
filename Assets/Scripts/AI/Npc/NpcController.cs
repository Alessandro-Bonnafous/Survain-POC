using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Survain.Core;
using Survain.Gameplay.Buildings;
using Survain.Gameplay.Inventories;
using Survain.Gameplay.World;
using Survain.Items;

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

        [Tooltip("Métier initial du PNJ (#14). Le contremaître se règle ici sur le prefab dédié ; " +
                 "les villageois démarrent SansEmploi et reçoivent leur métier via le contremaître.")]
        [SerializeField] private NpcJob _job = NpcJob.SansEmploi;

        [Tooltip("Capacité de l'inventaire porté par le PNJ (ressources récoltées avant dépôt au coffre).")]
        [Min(1)]
        [SerializeField] private int _carriedCapacity = 4;

        public NPCData Data => _data;

        /// <summary>Inventaire porté par le PNJ (récolte transportée jusqu'au coffre, #14).</summary>
        public Inventory Carried { get; private set; }

        /// <summary>Métier courant du PNJ (#14). Pilote le comportement de travail (phase 2).</summary>
        public NpcJob Job => _job;
        public NavMeshAgent Agent { get; private set; }

        /// <summary>Animator du visuel (peut être null). Exposé pour les états (anim par état).</summary>
        public Animator Animator => _animator;

        /// <summary>Perception du PNJ (peut être null = aucune détection de menace).</summary>
        public NpcPerception Perception { get; private set; }

        /// <summary>Besoins du PNJ (peut être null = pas de faim/moral piloté).</summary>
        public NpcNeeds Needs { get; private set; }

        /// <summary>Point d'origine (lieu de spawn), centre de l'errance.</summary>
        public Vector3 HomePosition { get; private set; }

        // Registre statique des PNJ en scène (alternative à FindObjectsOfType). Consommé par
        // l'overlay d'état (#13 phase 3) et réutilisable par les futurs systèmes de village.
        private static readonly List<NpcController> _all = new List<NpcController>();
        public static IReadOnlyList<NpcController> All => _all;

        private INpcState _currentState;
        private Transform _talkTarget;        // cible à regarder en conversation (UI ouverte)
        private const float TalkTurnSpeed = 8f;

        /// <summary>Injecte la data (appelé par le spawner avant Start).</summary>
        public void SetData(NPCData data) => _data = data;

        /// <summary>Assigne un métier (appelé par le contremaître en phase 3, ou en DEBUG).</summary>
        public void SetJob(NpcJob job) => _job = job;

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Perception = GetComponent<NpcPerception>(); // optionnel
            Needs = GetComponent<NpcNeeds>();           // optionnel

            Carried = gameObject.AddComponent<Inventory>();
            Carried.ConfigureCapacity(_carriedCapacity);
        }

        private void OnEnable() => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

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
            // En conversation (UI du contremaître ouverte) : on fait face au joueur et on suspend
            // l'IA, pour vendre l'illusion du dialogue.
            if (_talkTarget != null) { FaceTalkTarget(); return; }

            // Interruptions globales prioritaires (les états n'ont pas à les tester eux-mêmes).
            // Ordre de priorité : fuite (survie) > désertion (terminal) > faim > nuit (repos) > travail.
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
            else if (Needs != null && Needs.IsHungry)
            {
                // Affamé sans feu de camp accessible : on cesse de travailler (passage en oisiveté),
                // et on ne reprend pas un métier tant que la faim n'est pas satisfaite.
                if (IsWorking(_currentState)) ChangeState(new IdleState());
            }
            else if (WorldClock.HasClock && WorldClock.IsNight && !IsForeman
                     && !(_currentState is EatingState))
            {
                // Nuit : routine de repos. La nuit interrompt le travail mais PAS un repas en cours
                // (EatingState s'auto-termine à satiété, comme le travail ne s'active que depuis
                // Idle/Wander) — sinon le PNJ quitte le feu dès que la faim repasse le seuil de
                // recherche, sans être rassasié, et boucle feu ⇄ foyer. Le contremaître est exempté
                // (il reste le point d'interaction du village, accessible aussi de nuit — ancre #14).
                if (!(_currentState is SleepingState)) ChangeState(new SleepingState());
            }
            else if (WorldClock.HasClock && WorldClock.IsMealTime && !IsForeman
                     && !(_currentState is EatingState))
            {
                // Créneau de repas : regroupement au feu. Interrompt le travail (et l'oisiveté),
                // mais pas un repas individuel déjà en cours (EatingState). Le contremaître reste
                // au village (hub). La sortie se fait dans le bloc « else » à la fin du créneau.
                if (!(_currentState is MealGatheringState)) ChangeState(new MealGatheringState());
            }
            else
            {
                // Jour ouvré, hors créneau, rassasié, pas de menace : on quitte une routine terminée
                // (réveil / fin de repas) puis on travaille (priorité la plus basse : rassasié et oisif).
                if (_currentState is SleepingState || _currentState is MealGatheringState)
                    ChangeState(new IdleState());
                if (IsWorkingJob(_job) && Carried != null) UpdateWork();
            }

            _currentState?.Tick(this);

            if (_animator != null && Agent != null)
            {
                _animator.SetFloat(NpcAnimParams.Speed, Agent.velocity.magnitude);
            }
        }

        /// <summary>Le contremaître : exempté des routines (reste le hub de gestion, #14).</summary>
        private bool IsForeman => _job == NpcJob.Contremaitre;

        /// <summary>Le PNJ est oisif et libre de discuter (idle social #15) : ni en conversation
        /// avec le joueur, ni occupé par un état prioritaire. Consommé par IdleState.</summary>
        public bool IsAvailableForChat => _talkTarget == null && _currentState is IdleState;

        /// <summary>Métiers de récolte (#14 phase 2A) : ciblent des nœuds par type d'outil.</summary>
        private static bool IsGatherJob(NpcJob job) => job == NpcJob.Bucheron || job == NpcJob.Mineur;

        /// <summary>Métiers qui occupent un état de travail (récolteurs + constructeur).</summary>
        private static bool IsWorkingJob(NpcJob job) => IsGatherJob(job) || job == NpcJob.Constructeur;

        /// <summary>Vrai si l'état courant est un état de travail (récolte ou construction).</summary>
        private static bool IsWorking(INpcState state) => state is GatherJobState || state is BuildJobState;

        /// <summary>Type d'outil ciblé par le métier de récolte (bûcheron → hache, mineur → pioche).</summary>
        private static ToolType ToolFor(NpcJob job) => job == NpcJob.Mineur ? ToolType.Pickaxe : ToolType.Axe;

        /// <summary>
        /// Entre/maintient l'état de métier approprié depuis l'oisiveté, et n'occupe un métier que
        /// s'il y a de quoi : un coffre pour les récolteurs (sinon récolter ne sert à rien), un
        /// chantier actif pour le constructeur. Sort vers l'oisiveté si la condition disparaît.
        /// </summary>
        private void UpdateWork()
        {
            if (_job == NpcJob.Constructeur)
            {
                if (!ConstructionSite.HasActive(transform.position, out _))
                {
                    if (_currentState is BuildJobState) ChangeState(new IdleState());
                }
                else if (_currentState is IdleState || _currentState is WanderState)
                {
                    ChangeState(new BuildJobState());
                }
            }
            else // récolteurs (bûcheron / mineur)
            {
                if (!GatherJobState.HasStorage(transform.position, out _))
                {
                    if (_currentState is GatherJobState) ChangeState(new IdleState());
                }
                else if (_currentState is IdleState || _currentState is WanderState)
                {
                    ChangeState(new GatherJobState(ToolFor(_job)));
                }
            }
        }

        /// <summary>Transition vers un nouvel état (Exit de l'ancien, Enter du nouveau).</summary>
        public void ChangeState(INpcState next)
        {
            _currentState?.Exit(this);
            _currentState = next;
            _currentState?.Enter(this);
        }

        /// <summary>Entre en conversation : le PNJ se fige et fait face à <paramref name="target"/>
        /// (appelé par l'UI du contremaître à l'ouverture). Suspend l'IA tant que c'est actif.</summary>
        public void BeginTalk(Transform target)
        {
            _talkTarget = target;
            if (Agent != null && Agent.isOnNavMesh) Agent.isStopped = true;
            if (Agent != null) Agent.updateRotation = false; // on pilote la rotation à la main
        }

        /// <summary>Fin de conversation : reprise propre de l'IA (appelé à la fermeture de l'UI).</summary>
        public void EndTalk()
        {
            if (_talkTarget == null) return;
            _talkTarget = null;
            if (Agent != null) Agent.updateRotation = true;
            ChangeState(new IdleState());
        }

        private void FaceTalkTarget()
        {
            if (Agent != null && Agent.isOnNavMesh) Agent.isStopped = true;

            Vector3 dir = _talkTarget.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * TalkTurnSpeed);

            if (_animator != null)
            {
                _animator.SetFloat(NpcAnimParams.Speed, 0f);     // pose idle
                _animator.SetBool(NpcAnimParams.Working, false);
            }
        }

        // --- DEBUG (#12) : forcer un état en jeu pour valider anim + transitions, en attendant
        // que les besoins (#13), métiers (#14) et routines (#15) les déclenchent réellement.
        // Sélectionner un PNJ spawné en Play, puis clic droit sur le composant → menu.
        [ContextMenu("DEBUG/Forcer Working")] private void DebugWorking() => ChangeState(new WorkingState());
        [ContextMenu("DEBUG/Forcer Eating")] private void DebugEating() => ChangeState(new EatingState());
        [ContextMenu("DEBUG/Forcer Sleeping")] private void DebugSleeping() => ChangeState(new SleepingState());
        [ContextMenu("DEBUG/Forcer Idle")] private void DebugIdle() => ChangeState(new IdleState());

        // --- DEBUG (#14) : assigner un métier en attendant le panneau du contremaître (phase 3).
        [ContextMenu("DEBUG/Métier — Bûcheron")] private void DebugJobBucheron() => SetJob(NpcJob.Bucheron);
        [ContextMenu("DEBUG/Métier — Mineur")] private void DebugJobMineur() => SetJob(NpcJob.Mineur);
        [ContextMenu("DEBUG/Métier — Constructeur")] private void DebugJobConstructeur() => SetJob(NpcJob.Constructeur);
        [ContextMenu("DEBUG/Métier — Sans emploi")] private void DebugJobSansEmploi() => SetJob(NpcJob.SansEmploi);
    }
}
