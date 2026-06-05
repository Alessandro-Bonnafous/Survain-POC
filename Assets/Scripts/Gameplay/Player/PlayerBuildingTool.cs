using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Survain.Core;
using Survain.Gameplay.Buildings;
using Survain.Gameplay.Interaction;
using Survain.Gameplay.Inventories;
using Survain.Items;
using Survain.UI;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Démolition : le clic gauche (Attack) inflige des dégâts au bâtiment visé (raycast
    /// caméra, comme la récolte). À 0 HP, le Building gère sa destruction (remboursement +
    /// feedback). Source de dégâts du POC ; le combat (Sprint 4) en fournira d'autres.
    ///
    /// Coexiste avec PlayerHarvester (qui ne cible que les ResourceNode) sur la même touche :
    /// chacun n'agit que sur son type de cible, mutuellement exclusifs par le raycast.
    /// Suspendu en mode construction (clic gauche = poser). Le prompt n'est affiché que pour
    /// les bâtiments NON interactables — les coffres gardent leur prompt « [E] Ouvrir »
    /// (PlayerInteractor) ; la démolition y reste fonctionnelle, juste sans prompt dédié.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerBuildingTool : MonoBehaviour
    {
        [Header("Dépendances")]
        [Tooltip("Asset Input System partagé. La map 'Player' doit exposer 'Attack'.")]
        [SerializeField] private InputActionAsset _inputActions;

        [Tooltip("Transform de la caméra (origine du raycast).")]
        [SerializeField] private Transform _cameraTransform;

        [Tooltip("Racine du joueur (ses colliders sont ignorés par le raycast). Si null = self.")]
        [SerializeField] private Transform _playerRoot;

        [Tooltip("Mode construction (optionnel). Quand il est actif, la démolition est suspendue.")]
        [SerializeField] private BuildModeController _buildMode;

        [Tooltip("Sac à dos : source des ressources consommées pour la réparation.")]
        [SerializeField] private Inventory _backpack;

        [Header("Démolition")]
        [Tooltip("Portée maximale du raycast (mètres).")]
        [Range(1f, 20f)]
        [SerializeField] private float _maxReach = 6f;

        [Tooltip("Dégâts infligés par coup.")]
        [Min(1)]
        [SerializeField] private int _damagePerHit = 25;

        [Tooltip("Délai minimum entre deux coups (secondes).")]
        [Min(0f)]
        [SerializeField] private float _hitCooldown = 0.4f;

        [Tooltip("Facteur de coût de réparation : coût = coût de construction × (dégâts) × ce facteur.")]
        [Min(0f)]
        [SerializeField] private float _repairCostFactor = 1f;

        private const string ActionMapName = "Player";
        private const string AttackActionName = "Attack";
        private const string RepairActionName = "Repair";

        private InputAction _attackAction;
        private InputAction _repairAction;
        private Building _currentTarget;
        private bool _showingPrompt;
        private float _nextHitAllowedAt;

        private void Awake()
        {
            if (_inputActions == null || _cameraTransform == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerBuildingTool : inputActions ou cameraTransform non assigné.", this);
                enabled = false;
                return;
            }

            if (_playerRoot == null) _playerRoot = transform;

            var map = _inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
            _attackAction = map?.FindAction(AttackActionName, throwIfNotFound: false);
            _repairAction = map?.FindAction(RepairActionName, throwIfNotFound: false);
            if (_attackAction == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    $"PlayerBuildingTool : action '{AttackActionName}' introuvable dans la map '{ActionMapName}'.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_attackAction != null) _attackAction.started += OnAttack;
            if (_repairAction != null) _repairAction.started += OnRepair;
        }

        private void OnDisable()
        {
            if (_attackAction != null) _attackAction.started -= OnAttack;
            if (_repairAction != null) _repairAction.started -= OnRepair;
        }

        private void Update()
        {
            // Suspendu en mode construction (clic gauche = poser) ET quand un panneau UI est ouvert
            // (sinon un clic sur un slot d'inventaire/coffre démolit le bâtiment visé « à travers » l'UI).
            if ((_buildMode != null && _buildMode.IsActive) || UiMode.IsActive)
            {
                SetTarget(null);
                if (_showingPrompt) { InteractionPrompt.Instance.Hide(); _showingPrompt = false; }
                return;
            }

            SetTarget(RaycastForBuilding());
            RefreshPrompt();
        }

        private void OnDestroy()
        {
            if (_currentTarget != null) _currentTarget.SetHighlighted(false);
            if (_showingPrompt) InteractionPrompt.Instance.Hide();
        }

        private void SetTarget(Building target)
        {
            if (target == _currentTarget) return;
            if (_currentTarget != null) _currentTarget.SetHighlighted(false);
            _currentTarget = target;
            if (_currentTarget != null) _currentTarget.SetHighlighted(true);
        }

        private void RefreshPrompt()
        {
            // On n'affiche le prompt que pour les bâtiments non interactables (les coffres
            // gardent leur prompt d'ouverture via PlayerInteractor). On ne masque que si
            // c'est NOUS qui affichions, pour ne pas écraser le prompt d'un autre système.
            bool shouldShow = _currentTarget != null && !HasInteractable(_currentTarget);
            if (shouldShow)
            {
                var b = _currentTarget;
                var d = b.Data;
                string label = d != null ? d.DisplayName : "Structure";
                string prompt = $"[Clic gauche] Démolir {label}  ·  {b.CurrentHp}/{b.MaxHp} PV";
                if (b.CurrentHp < b.MaxHp)
                {
                    string costStr = RepairCostLabel(b);
                    prompt += $"  ·  [R] Réparer ({costStr})";
                }
                InteractionPrompt.Instance.Show(prompt);
                _showingPrompt = true;
            }
            else if (_showingPrompt)
            {
                InteractionPrompt.Instance.Hide();
                _showingPrompt = false;
            }
        }

        private void OnAttack(InputAction.CallbackContext _)
        {
            if ((_buildMode != null && _buildMode.IsActive) || UiMode.IsActive) return;
            if (_currentTarget == null) return;
            if (Time.time < _nextHitAllowedAt) return;

            _nextHitAllowedAt = Time.time + _hitCooldown;
            _currentTarget.TakeDamage(_damagePerHit);
            // Le Building gère sa destruction à 0 HP ; le prochain raycast lâchera la cible.
        }

        private void OnRepair(InputAction.CallbackContext _)
        {
            if ((_buildMode != null && _buildMode.IsActive) || UiMode.IsActive) return;
            if (_currentTarget == null) return;

            var b = _currentTarget;
            if (b.CurrentHp >= b.MaxHp) return; // pas endommagé

            if (_backpack == null)
            {
                SurvainLog.Warn(SurvainLog.Category.Gameplay,
                    "PlayerBuildingTool : backpack non assigné, réparation impossible.", this);
                return;
            }

            var cost = ComputeRepairCost(b);
            foreach (var c in cost)
            {
                if (_backpack.Count(c.item) < c.amount)
                {
                    SurvainLog.Info(SurvainLog.Category.Gameplay,
                        "Réparation : ressources manquantes.", this);
                    return;
                }
            }
            foreach (var c in cost) _backpack.TryRemove(c.item, c.amount);

            b.Repair(b.MaxHp - b.CurrentHp); // réparation complète
            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Bâtiment '{(b.Data != null ? b.Data.Id : "?")}' réparé.", this);
        }

        /// <summary>Coût de réparation : coût de construction × fraction de dégâts × facteur.</summary>
        private List<(ItemData item, int amount)> ComputeRepairCost(Building b)
        {
            var result = new List<(ItemData, int)>();
            var data = b.Data;
            if (data == null || data.Cost == null) return result;

            float missingFrac = b.MaxHp > 0 ? 1f - (float)b.CurrentHp / b.MaxHp : 0f;
            foreach (var c in data.Cost)
            {
                if (c.Item == null) continue;
                int need = Mathf.CeilToInt(c.Amount * missingFrac * _repairCostFactor);
                if (need > 0) result.Add((c.Item, need));
            }
            return result;
        }

        private string RepairCostLabel(Building b)
        {
            var cost = ComputeRepairCost(b);
            if (cost.Count == 0) return "gratuit";
            var parts = new List<string>(cost.Count);
            foreach (var c in cost)
            {
                string l = string.IsNullOrEmpty(c.item.DisplayName) ? c.item.Id : c.item.DisplayName;
                parts.Add($"{c.amount} {l}");
            }
            return string.Join(", ", parts);
        }

        private Building RaycastForBuilding()
        {
            // Portée mesurée DEPUIS LE JOUEUR (cf. PlayerInteractor) : on ajoute la distance
            // caméra→joueur pour que _maxReach reste la vraie portée devant le perso, quel que soit le zoom.
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

                var building = hits[i].collider.GetComponentInParent<Building>();
                return building; // premier hit non-joueur : Building ou rien (vue obstruée)
            }
            return null;
        }

        private static bool HasInteractable(Building building)
        {
            return building.GetComponent<IInteractable>() != null;
        }
    }
}
