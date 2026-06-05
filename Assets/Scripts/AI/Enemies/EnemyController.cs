using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Survain.Core;
using Survain.Gameplay.Inventories;
using Survain.Gameplay.Player;

namespace Survain.AI.Enemies
{
    /// <summary>
    /// Runtime d'un ennemi PVE (#17) : consomme une <see cref="EnemyData"/>, pilote un NavMeshAgent
    /// et une machine à états polymorphe (Patrol / Chase / Attack / Return). Même architecture que
    /// NpcController (registre statique, NavMesh autorité de position, Animator optionnel piloté par
    /// "Speed").
    ///
    /// L'aggro/désaggro est centralisé ici (les états ne le testent pas) : on cible le joueur
    /// (<see cref="PlayerController.Instance"/>) — pas de FindObjectOfType. Le GameObject doit être
    /// sur le layer Threat pour que les PNJ le fuient (NpcPerception, #12).
    ///
    /// Phase 1 : locomotion + aggro + attaque en telegraph (sans dégâts). HP/mort/loot = phase 2.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class EnemyController : MonoBehaviour
    {
        [Tooltip("Données de l'ennemi (vitesse, aggro, combat, loot). Injectable par le spawner.")]
        [SerializeField] private EnemyData _data;

        [Tooltip("Animator du visuel (optionnel). Reçoit 'speed' pour la locomotion.")]
        [SerializeField] private Animator _animator;

        public EnemyData Data => _data;
        public NavMeshAgent Agent { get; private set; }
        public Animator Animator => _animator;

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }

        /// <summary>Émis quand l'ennemi meurt, juste avant sa destruction (consommé par le spawner
        /// pour le respawn, #17 phase 2B). Signature : l'ennemi qui meurt.</summary>
        public event Action<EnemyController> Died;

        private bool _dead;

        /// <summary>Point d'origine (spawn), centre de la patrouille et ancre de la laisse.</summary>
        public Vector3 HomePosition { get; private set; }

        private static readonly List<EnemyController> _all = new List<EnemyController>();
        public static IReadOnlyList<EnemyController> All => _all;

        private static readonly int SpeedHash = Animator.StringToHash("speed");

        private IEnemyState _currentState;

        public void SetData(EnemyData data) => _data = data;

        /// <summary>Cible courante : le joueur s'il existe (null sinon). Phase 1 : joueur uniquement.</summary>
        public Transform Target => PlayerController.Instance != null ? PlayerController.Instance.transform : null;

        private void Awake() => Agent = GetComponent<NavMeshAgent>();

        private void OnEnable() => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

        private void Start()
        {
            if (_data == null)
            {
                SurvainLog.Error(SurvainLog.Category.AI, "EnemyController : EnemyData manquant.", this);
                enabled = false;
                return;
            }

            Agent.speed = _data.PatrolSpeed;
            MaxHp = Mathf.Max(1, _data.MaxHp);
            CurrentHp = MaxHp;
            HomePosition = transform.position; // après positionnement par le spawner
            ChangeState(new EnemyPatrolState());
        }

        /// <summary>Inflige des dégâts (clampés à 0). À 0 HP, l'ennemi meurt (loot + destruction).
        /// Source POC : clic gauche (PlayerEnemyStrike) ; le combat #16 la remplacera.</summary>
        public void TakeDamage(int amount)
        {
            if (_dead || amount <= 0 || CurrentHp <= 0) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            SurvainLog.Info(SurvainLog.Category.AI, $"{name} touché : {CurrentHp}/{MaxHp} PV.", this);
            if (CurrentHp == 0) Die();
        }

        private void Die()
        {
            if (_dead) return;
            _dead = true;

            // Loot : déverse les items de la table au sol (ramassables via WorldItem).
            Vector3 pos = transform.position + Vector3.up * 0.5f;
            var loot = _data.Loot;
            if (loot != null)
            {
                for (int i = 0; i < loot.Count; i++)
                {
                    if (loot[i].Item != null && loot[i].Amount > 0)
                        WorldItemSpawner.Spawn(loot[i].Item, loot[i].Amount, pos);
                }
            }

            SurvainLog.Info(SurvainLog.Category.AI, $"{name} tué.", this);
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        private void Update()
        {
            // Aggro/désaggro centralisé (les états ne le testent pas).
            var target = Target;
            bool engaged = _currentState is EnemyChaseState || _currentState is EnemyAttackState;

            if (!engaged)
            {
                // Patrouille / retour : on engage si le joueur entre dans le rayon d'aggro.
                if (_data.Aggressive && target != null
                    && PlanarDistance(target.position, transform.position) <= _data.AggroRadius)
                {
                    ChangeState(new EnemyChaseState());
                }
            }
            else
            {
                // Engagé : on abandonne si la cible disparaît, sort du rayon de désaggro, ou si l'on
                // s'est trop éloigné du foyer (laisse).
                bool lost = target == null
                            || PlanarDistance(target.position, transform.position) > _data.DeaggroRadius
                            || PlanarDistance(transform.position, HomePosition) > _data.LeashRadius;
                if (lost) ChangeState(new EnemyReturnState());
            }

            _currentState?.Tick(this);

            if (_animator != null && Agent != null)
                _animator.SetFloat(SpeedHash, Agent.velocity.magnitude);
        }

        /// <summary>Transition d'état (Exit de l'ancien, Enter du nouveau).</summary>
        public void ChangeState(IEnemyState next)
        {
            _currentState?.Exit(this);
            _currentState = next;
            _currentState?.Enter(this);
        }

        /// <summary>Distance planaire (XZ) — ignore la hauteur pour des seuils d'aggro stables sur terrain pentu.</summary>
        public static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_data == null) return;
            Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, _data.AggroRadius);
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, _data.DeaggroRadius);
        }
#endif
    }
}
