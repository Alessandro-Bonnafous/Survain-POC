using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Survain.Core;
using Survain.AI.Enemies;
using Survain.Gameplay.Buildings;
using Survain.Items;
using Survain.UI;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// PLACEHOLDER de combat (#17/#16) — en attendant le vrai système d'endurance (#16, décalé).
    /// Le clic gauche (action Attack) fait un raycast caméra et inflige des dégâts à l'ennemi visé.
    /// **Les dégâts ne passent que si une arme est équipée** (hache ou pioche au POC) ; un coup réussi
    /// émet <see cref="Swung"/> → l'avatar joue l'anim de l'outil (Chop/Mine via PlayerVisualAnimator).
    /// Coexiste avec PlayerHarvester (nœuds) et PlayerBuildingTool (bâtiments), exclusifs par le type
    /// de cible. Neutralisé sous l'UI (UiMode.IsActive) et en mode construction. La vraie mécanique de
    /// combat remplacera ce placeholder en #16.
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

        [Tooltip("Portée de la frappe, mesurée devant le joueur (mètres).")]
        [Range(1f, 20f)]
        [SerializeField] private float _maxReach = 4f;

        [Tooltip("Dégâts infligés par coup (placeholder).")]
        [Min(1)]
        [SerializeField] private int _damagePerHit = 10;

        [Tooltip("Délai minimum entre deux coups (secondes).")]
        [Min(0f)]
        [SerializeField] private float _hitCooldown = 0.4f;

        private const string ActionMapName = "Player";
        private const string AttackActionName = "Attack";

        /// <summary>Émis à chaque coup d'arme réussi sur un ennemi. Consommé par PlayerVisualAnimator
        /// pour jouer l'anim de l'outil équipé (Chop/Mine), comme HitLanded pour la récolte.</summary>
        public event Action Swung;

        private InputAction _attackAction;
        private float _nextHitAllowedAt;

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
            if (enemy == null) return;

            _nextHitAllowedAt = Time.time + _hitCooldown;
            enemy.TakeDamage(_damagePerHit);
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
