using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Survain.Core;
using Survain.AI.Enemies;
using Survain.Gameplay.Buildings;
using Survain.Gameplay.Combat;
using Survain.Items;
using Survain.UI;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Auto-attack du joueur (combat #16, Phase A / A2) : devient la vraie source de dégâts joueur,
    /// désormais <b>pilotée par l'énergie</b> — chaque coup porté consomme <see cref="_energyCostPerAttack"/>
    /// (spec : 5 % de la réserve) via <see cref="PlayerEnergy.TryConsume"/> ; à sec, le coup ne part pas.
    /// Le clic gauche (action Attack) fait un raycast caméra et inflige des dégâts à l'ennemi visé.
    /// **Les dégâts ne passent que si une arme est équipée** (hache ou pioche au POC) ; un coup réussi
    /// émet <see cref="Swung"/> → l'avatar joue l'anim de l'outil (Chop/Mine via PlayerVisualAnimator).
    /// Coexiste avec PlayerHarvester (nœuds) et PlayerBuildingTool (bâtiments), exclusifs par le type
    /// de cible. Neutralisé sous l'UI (UiMode.IsActive) et en mode construction.
    ///
    /// Énergie consommée <b>uniquement sur un coup de combat</b> (ennemi visé), pas sur un clic à vide
    /// (qui peut être une récolte). Les dégâts/cooldown restent des placeholders sur le composant : ils
    /// migreront sur <c>WeaponData</c> avec les vraies armes craftables (Phase B).
    ///
    /// Phase B / B4 (#84) : le coup est désormais <b>typé</b> — son total est décomposé en part de biome
    /// + part physique (spec : 80/20, placeholder ajustable <see cref="_biomeDamageFraction"/>) via
    /// <see cref="DamageInfo.Split"/>, puis appliqué par <see cref="EnemyController.TakeDamage(DamageInfo)"/>.
    /// Le biome/split vivent ici en placeholders (les armes du POC sont des outils hache/pioche) ; quand le
    /// craft #8 équipera de vraies <c>WeaponData</c>, on lira <see cref="WeaponData.BuildHit"/> à la place
    /// (le crochet existe déjà sur WeaponData).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerEnemyStrike : MonoBehaviour
    {
        [Tooltip("Asset Input System partagé. La map 'Player' doit exposer 'Attack'.")]
        [SerializeField] private InputActionAsset _inputActions;

        [Tooltip("Transform de la caméra (origine du raycast).")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Racine du joueur (ses colliders sont ignorés par le raycast). Si null = self.")]
        [SerializeField] private Transform _playerRoot;

        [Tooltip("Mode construction (optionnel). Quand il est actif, la frappe est suspendue.")]
        [SerializeField] private BuildModeController _buildMode;

        [Tooltip("Équipement joueur. Si assigné, seuls les outils-armes (hache/pioche) infligent des " +
            "dégâts. Si null = comportement permissif (tout clic blesse).")]
        [SerializeField] private PlayerEquipment _equipment;

        [Tooltip("Réserve d'énergie (#81). Auto-résolue sur le même GameObject si laissé vide. " +
            "Si null = pas de coût en énergie (fallback permissif).")]
        [SerializeField] private PlayerEnergy _energy;

        [Tooltip("Énergie consommée par coup de combat (spec : 5 % de la réserve = 5). Placeholder " +
            "ajustable ; migrera sur WeaponData en Phase B (#84).")]
        [Min(0f)]
        [SerializeField] private float _energyCostPerAttack = 5f;

        [Tooltip("Portée de la frappe, mesurée devant le joueur (mètres).")]
        [Range(1f, 20f)]
        [SerializeField] private float _maxReach = 4f;

        [Tooltip("Dégâts totaux infligés par coup (placeholder ; répartis biome/physique selon le split).")]
        [Min(1)]
        [SerializeField] private int _damagePerHit = 10;

        [Tooltip("Délai minimum entre deux coups (secondes).")]
        [Min(0f)]
        [SerializeField] private float _hitCooldown = 0.4f;

        [Header("Dégâts typés (#16 B4 — placeholders, migreront sur WeaponData)")]
        [Tooltip("Biome de l'arme courante (part principale du coup). Placeholder ajustable (#88).")]
        [SerializeField] private DamageType _biomeDamageType = DamageType.ForetTemperee;

        [Tooltip("Part de dégâts de biome dans le total (spec Q2 : 0.8 = 80 % biome / 20 % physique). "
            + "Placeholder ajustable (#88).")]
        [Range(0f, 1f)]
        [SerializeField] private float _biomeDamageFraction = 0.8f;

        private const string ActionMapName = "Player";
        private const string AttackActionName = "Attack";

        /// <summary>Émis à chaque coup d'arme réussi sur un ennemi. Consommé par PlayerVisualAnimator
        /// pour jouer l'anim de l'outil équipé (Chop/Mine), comme HitLanded pour la récolte.</summary>
        public event Action Swung;

        private InputAction _attackAction;
        private float _nextHitAllowedAt;
        private float _nextEmptyFeedbackAt;

        private void Awake()
        {
            if (_inputActions == null || _cameraTransform == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerEnemyStrike : inputActions ou cameraTransform non assigné.", this);
                enabled = false;
                return;
            }

            if (_playerRoot == null) _playerRoot = transform;
            if (_equipment == null) _equipment = GetComponentInChildren<PlayerEquipment>();
            if (_energy == null) _energy = GetComponent<PlayerEnergy>();

            var map = _inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            _attackAction = map?.FindAction(AttackActionName, throwIfNotFound: false);
            if (_attackAction == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    $"PlayerEnemyStrike : action '{AttackActionName}' introuvable dans la map '{ActionMapName}'.", this);
                enabled = false;
            }
        }

        private void OnEnable() { if (_attackAction != null) _attackAction.started += OnAttack; }
        private void OnDisable() { if (_attackAction != null) _attackAction.started -= OnAttack; }

        private void OnAttack(InputAction.CallbackContext _)
        {
            if ((_buildMode != null && _buildMode.IsActive) || UiMode.IsActive) return;
            if (Time.time < _nextHitAllowedAt) return;
            if (!IsWeaponEquipped()) return; // seules les armes (hache/pioche au POC) infligent des dégâts

            var enemy = RaycastForEnemy();
            if (enemy == null) return; // clic à vide : pas un coup de combat → pas de coût en énergie

            // Coup de combat : il faut assez d'énergie (spec : 5 %). À sec, le coup ne part pas.
            if (_energy != null && !_energy.TryConsume(_energyCostPerAttack))
            {
                if (Time.time >= _nextEmptyFeedbackAt)
                {
                    _nextEmptyFeedbackAt = Time.time + 0.5f; // throttle anti-spam du feedback
                    SurvainLog.Info(SurvainLog.Category.Gameplay,
                        "Pas assez d'énergie pour attaquer.", this);
                }
                return;
            }

            _nextHitAllowedAt = Time.time + _hitCooldown;

            // Coup typé (B4) : décompose le total en part biome + part physique (spec 80/20).
            // Quand le craft #8 équipera de vraies WeaponData, lire weapon.BuildHit() à la place.
            var hit = DamageInfo.Split(_damagePerHit, _biomeDamageFraction, _biomeDamageType);
            enemy.TakeDamage(hit);
            Swung?.Invoke(); // déclenche l'anim de l'outil équipé (Chop/Mine)
        }

        /// <summary>Vrai si l'outil équipé peut servir d'arme (hache/pioche au POC). Sans référence
        /// d'équipement assignée, reste permissif (placeholder).</summary>
        private bool IsWeaponEquipped()
        {
            if (_equipment == null) return true;
            var tool = _equipment.CurrentTool;
            return tool != null && (tool.ToolType == ToolType.Axe || tool.ToolType == ToolType.Pickaxe);
        }

        private EnemyController RaycastForEnemy()
        {
            // Portée mesurée DEPUIS LE JOUEUR (cf. PlayerBuildingTool) : on ajoute la distance
            // caméra→joueur pour que _maxReach reste la vraie portée devant le perso, zoom inclus.
            float camToPlayer = Vector3.Distance(_cameraTransform.position, _playerRoot.position);
            var hits = Physics.RaycastAll(
                _cameraTransform.position, _cameraTransform.forward,
                camToPlayer + _maxReach, ~0, QueryTriggerInteraction.Ignore);
            if (hits.Length == 0) return null;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                var t = hits[i].collider.transform;
                if (t == _playerRoot || t.IsChildOf(_playerRoot)) continue;
                return hits[i].collider.GetComponentInParent<EnemyController>(); // 1er hit non-joueur : ennemi ou rien
            }
            return null;
        }
    }
}
