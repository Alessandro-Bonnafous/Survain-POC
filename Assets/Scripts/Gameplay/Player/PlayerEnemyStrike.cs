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
    ///
    /// <para><b>Synchro anim/dégât (polish Phase B).</b> Le clic ne fait plus de dégât immédiat : il
    /// <b>lance un swing</b> (l'anim part via <see cref="Swung"/>) et <b>verrouille</b> les attaques
    /// (<see cref="IsSwinging"/>) → <b>1 swing = 1 coup</b> (fini les 3-4 dégâts pour 2 anims).
    /// <list type="bullet">
    /// <item><b>Impact piloté par l'animation</b> : un Animation Event <b>à la frame de contact</b> appelle
    /// <see cref="NotifyAnimationImpact"/> → dégât appliqué <b>une seule fois</b> (re-raycast au contact :
    /// coup dans le vide si la cible s'est échappée). C'est calé <b>par clip</b> (hache ≠ pioche).</item>
    /// <item><b>Verrou/cadence piloté par une durée gameplay</b> (<see cref="_swingDurationSeconds"/>, =
    /// vitesse d'attaque). Volontairement <b>pas</b> dérivé d'un event de fin de clip : un tel event, mal
    /// placé, libérait le verrou trop tôt → swings qui se relancent en martelant. Si l'event d'impact est
    /// absent, le coup tombe en fin de ce délai (filet de sécurité).</item>
    /// </list>
    /// Le relais de l'event passe par <see cref="PlayerAttackAnimationRelay"/> (l'Animator est sur l'avatar
    /// enfant). Le verrou <see cref="IsSwinging"/> est le <b>socle réutilisable</b> par le futur coordinateur
    /// de compétences (B6).</para>
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

        [Header("Synchro anim/dégât (#16)")]
        [Tooltip("Durée de verrouillage du swing = CADENCE d'attaque : le clic est bloqué pendant ce temps, "
            + "donc marteler ne peut pas enchaîner plus vite. À régler ≈ la durée du clip d'attaque le plus "
            + "long (sinon l'anim peut se relancer par-dessus). L'IMPACT, lui, est calé par clip via "
            + "l'Animation Event AnimImpact (hache ≠ pioche). Si AnimImpact est absent, le coup tombe en fin "
            + "de ce délai (filet de sécurité). Placeholder ajustable (#88), migrera en vitesse d'attaque "
            + "sur WeaponData.")]
        [Min(0.1f)]
        [SerializeField] private float _swingDurationSeconds = 0.8f;

        [Header("Dégâts typés (#16 B4 — placeholders, migreront sur WeaponData)")]
        [Tooltip("Biome par défaut (fallback). Au POC, le biome dépend de l'outil équipé : hache → Forêt, "
            + "pioche → Montagnes (cf. ResolveBiomeType). Placeholder ajustable (#88).")]
        [SerializeField] private DamageType _biomeDamageType = DamageType.Foret;

        [Tooltip("Part de dégâts de biome dans le total (spec Q2 : 0.8 = 80 % biome / 20 % physique). "
            + "Placeholder ajustable (#88).")]
        [Range(0f, 1f)]
        [SerializeField] private float _biomeDamageFraction = 0.8f;

        private const string ActionMapName = "Player";
        private const string AttackActionName = "Attack";

        /// <summary>Émis au <b>début</b> d'un swing (un ennemi était visé et l'énergie a été consommée).
        /// Consommé par PlayerVisualAnimator pour jouer l'anim de l'outil équipé (Chop/Mine). L'anim et le
        /// dégât partent ainsi du même instant ; le dégât tombe lui à la frame de contact.</summary>
        public event Action Swung;

        private InputAction _attackAction;
        private float _nextEmptyFeedbackAt;

        // ─── Swing en cours (synchro anim/dégât) ───────────────────────────
        private bool _swingActive;
        private bool _impactApplied;
        private float _swingStartedAt;

        /// <summary>Vrai pendant un swing (du clic jusqu'à la fin du clip / filet de sécurité). Verrou
        /// « le joueur est en train de frapper » — socle réutilisable par le coordinateur de compétences
        /// (B6) pour exclure auto-attack et compétences entre elles.</summary>
        public bool IsSwinging => _swingActive;

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

        private void OnDisable()
        {
            if (_attackAction != null) _attackAction.started -= OnAttack;
            _swingActive = false; // ne pas reprendre un swing figé au ré-enable (respawn, etc.)
        }

        private void OnAttack(InputAction.CallbackContext _)
        {
            if ((_buildMode != null && _buildMode.IsActive) || UiMode.IsActive) return;
            if (_swingActive) return;        // déjà en plein swing : verrouillé → 1 swing = 1 coup + cadence
            if (!IsWeaponEquipped()) return; // seules les armes (hache/pioche au POC) frappent

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

            // Lance le swing : l'anim part maintenant ; le dégât tombera à la frame de contact, signalée
            // par l'Animation Event AnimImpact (NotifyAnimationImpact). Le swing reste verrouillé pendant
            // _swingDurationSeconds (cadence) → le martelage ne peut pas relancer un swing plus tôt.
            _swingActive = true;
            _impactApplied = false;
            _swingStartedAt = Time.time;
            Swung?.Invoke(); // déclenche l'anim de l'outil équipé (Chop/Mine)
        }

        /// <summary>Appelé par l'Animation Event de contact (via <see cref="PlayerAttackAnimationRelay"/>).
        /// Applique le dégât du swing en cours, une seule fois. Ignoré hors swing de combat (le clip
        /// Chop/Mine sert aussi à la récolte → l'event s'y déclenche mais ne doit rien faire ici).</summary>
        public void NotifyAnimationImpact()
        {
            if (!_swingActive || _impactApplied) return;
            _impactApplied = true;
            ApplyImpact();
        }

        /// <summary>⚠️ Déprécié — conservé en no-op pour ne pas casser un éventuel Animation Event
        /// "AnimSwingEnd" déjà posé sur un clip (sinon Unity logge "no receiver"). Le déverrouillage est
        /// désormais piloté par la durée (<see cref="_swingDurationSeconds"/>), pas par un event de fin :
        /// un event de fin mal placé libérait le verrou trop tôt → swings qui se relancent en martelant.</summary>
        public void NotifyAnimationSwingEnd() { /* no-op : déverrouillage par durée, cf. Update */ }

        private void Update()
        {
            // Le swing reste verrouillé _swingDurationSeconds (cadence). À l'échéance : on déverrouille, et
            // si l'Animation Event AnimImpact n'a pas appliqué le coup (clip sans event), filet de sécurité.
            if (!_swingActive) return;
            if (Time.time < _swingStartedAt + _swingDurationSeconds) return;

            if (!_impactApplied) { _impactApplied = true; ApplyImpact(); }
            _swingActive = false;
        }

        /// <summary>Applique le dégât du swing à l'instant de contact : re-vise l'ennemi devant la hache
        /// (il a pu mourir/esquiver/sortir de portée depuis le début du swing → coup dans le vide).</summary>
        private void ApplyImpact()
        {
            if ((_buildMode != null && _buildMode.IsActive) || UiMode.IsActive) return; // swing avorté par l'UI
            var enemy = RaycastForEnemy();
            if (enemy == null) return; // le coup a fendu l'air

            // Coup typé (B4) : décompose le total en part biome + part physique (spec 80/20).
            // Quand le craft #8 équipera de vraies WeaponData, lire weapon.BuildHit() à la place.
            var hit = DamageInfo.Split(_damagePerHit, _biomeDamageFraction, ResolveBiomeType());
            enemy.TakeDamage(hit);
        }

        /// <summary>Biome du coup courant. Placeholder POC : dérivé de l'outil-arme équipé (hache → Forêt,
        /// pioche → Montagnes) pour visualiser deux types de dégâts distincts ; fallback sur
        /// <see cref="_biomeDamageType"/>. Migrera sur <c>WeaponData.BiomeDamageType</c> avec le craft #8.</summary>
        private DamageType ResolveBiomeType()
        {
            var tool = _equipment != null ? _equipment.CurrentTool : null;
            if (tool != null)
            {
                if (tool.ToolType == ToolType.Axe) return DamageType.Foret;
                if (tool.ToolType == ToolType.Pickaxe) return DamageType.Montagnes;
            }
            return _biomeDamageType;
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
