using UnityEngine;
using Survain.Items;

namespace Survain.AI.Enemies
{
    /// <summary>
    /// Données d'un type d'ennemi PVE (#17) : locomotion, perception/aggro, combat et loot.
    /// SO data pur consommé par <see cref="EnemyController"/> (même pattern que NPCData ↔
    /// NpcController). Namespace Survain.AI.Enemies.
    ///
    /// Phase 1 : seuls locomotion + aggro + telegraph d'attaque sont consommés (les ennemis
    /// patrouillent, chassent le joueur, font fuir les PNJ via le layer Threat). HP, dégâts et
    /// loot (champs déjà présents) sont branchés en phase 2 (mort par dégâts placeholder + drop).
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Survain/AI/Enemy Data", order = 81)]
    public sealed class EnemyData : ScriptableObject
    {
        [Header("Identité")]
        [SerializeField] private string _displayName = "Ennemi";

        [Header("Locomotion")]
        [Tooltip("Vitesse de patrouille (NavMeshAgent).")]
        [Min(0.1f)] [SerializeField] private float _patrolSpeed = 2.5f;

        [Tooltip("Vitesse de poursuite (NavMeshAgent) — typiquement plus rapide que la patrouille.")]
        [Min(0.1f)] [SerializeField] private float _chaseSpeed = 4.5f;

        [Tooltip("Rayon de patrouille autour du point d'origine (mètres).")]
        [Min(1f)] [SerializeField] private float _patrolRadius = 8f;

        [Tooltip("Durée min d'attente entre deux déplacements de patrouille (s).")]
        [Min(0f)] [SerializeField] private float _idlePauseMin = 1.5f;

        [Tooltip("Durée max d'attente entre deux déplacements de patrouille (s).")]
        [Min(0f)] [SerializeField] private float _idlePauseMax = 4f;

        [Header("Perception / aggro")]
        [Tooltip("Rayon d'acquisition de cible : le joueur entrant dans ce rayon déclenche la poursuite.")]
        [Min(0.5f)] [SerializeField] private float _aggroRadius = 9f;

        [Tooltip("Au-delà de ce rayon (cible), l'ennemi abandonne et rentre (désaggro). ≥ aggroRadius.")]
        [Min(0.5f)] [SerializeField] private float _deaggroRadius = 14f;

        [Tooltip("Distance max au point d'origine pendant une poursuite : au-delà, l'ennemi renonce et rentre (laisse).")]
        [Min(1f)] [SerializeField] private float _leashRadius = 20f;

        [Tooltip("Si vrai, l'ennemi aggro à vue. Si faux, il ne devient hostile qu'une fois attaqué (phase 2).")]
        [SerializeField] private bool _aggressive = true;

        [Header("Combat")]
        [Tooltip("Portée à laquelle l'ennemi passe en attaque (s'arrête et frappe).")]
        [Min(0.5f)] [SerializeField] private float _attackRange = 2f;

        [Tooltip("Durée du telegraph d'attaque (windup visible). Phase 1 : sans dégâts.")]
        [Min(0.1f)] [SerializeField] private float _attackTelegraphSeconds = 0.8f;

        [Tooltip("Délai entre deux attaques (s).")]
        [Min(0.1f)] [SerializeField] private float _attackCooldown = 1.5f;

        [Tooltip("Dégâts par attaque (consommé au branchement du combat #16 / vie joueur #19).")]
        [Min(0)] [SerializeField] private int _attackDamage = 8;

        [Tooltip("Points de vie (consommé en phase 2 : mort par dégâts placeholder + loot).")]
        [Min(1)] [SerializeField] private int _maxHp = 30;

        [Header("Loot (phase 2)")]
        [Tooltip("Items lâchés à la mort (matériaux sauvages). Consommé en phase 2.")]
        [SerializeField] private BuildCost[] _loot;

        public string DisplayName => _displayName;
        public float PatrolSpeed => _patrolSpeed;
        public float ChaseSpeed => _chaseSpeed;
        public float PatrolRadius => _patrolRadius;
        public float IdlePauseMin => _idlePauseMin;
        public float IdlePauseMax => Mathf.Max(_idlePauseMin, _idlePauseMax);
        public float AggroRadius => _aggroRadius;
        public float DeaggroRadius => Mathf.Max(_aggroRadius, _deaggroRadius);
        public float LeashRadius => _leashRadius;
        public bool Aggressive => _aggressive;
        public float AttackRange => _attackRange;
        public float AttackTelegraphSeconds => _attackTelegraphSeconds;
        public float AttackCooldown => _attackCooldown;
        public int AttackDamage => _attackDamage;
        public int MaxHp => _maxHp;
        public System.Collections.Generic.IReadOnlyList<BuildCost> Loot => _loot;
    }
}
