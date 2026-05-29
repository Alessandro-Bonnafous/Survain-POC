using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Survain.Core;
using Survain.UI;

namespace Survain.Gameplay.Inventories
{
    /// <summary>
    /// Zone trigger sphérique qui détecte les WorldItem entrant en contact et permet leur
    /// ramassage manuel via une action input (touche F dans le binding actuel) — issue #40.
    ///
    /// La zone maintient la liste des WorldItem en proximité, leur active la surbrillance
    /// émissive (cf. WorldItem.SetHighlighted), et affiche un prompt "[F] Ramasser X" tant
    /// qu'au moins un item est en zone.
    ///
    /// Toggle `_autoPickup` exposé pour repasser en mode auto si besoin (legacy phase 1).
    /// En mode auto, le ramassage est instantané à l'entrée dans la zone, sans input.
    ///
    /// Le libellé du prompt est codé en dur sur "[F]" — à muter si le binding change.
    /// Une lecture dynamique de l'action (`DisplayString` du binding actif) serait possible
    /// mais sur-engineerée au stade POC.
    ///
    /// Convention : composant à poser sur un enfant de _Player (ex: _Player/PickupZone/) avec
    /// un SphereCollider en mode IsTrigger=true. La racine _Player a déjà un CharacterController
    /// non-trigger, donc ne peut pas porter le trigger directement.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class InventoryPickupZone : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Inventaire cible (typiquement le Backpack du joueur).")]
        [SerializeField] private Inventory _targetInventory;

        [Header("Input")]
        [Tooltip("Asset Input System partagé. L'action 'PickupItem' doit exister dans la map 'Player'.")]
        [SerializeField] private InputActionAsset _inputActions;

        [Header("Mode")]
        [Tooltip("Si true : ramassage automatique au contact (legacy phase 1). " +
                 "Si false : ramassage manuel via l'action PickupItem (E/F).")]
        [SerializeField] private bool _autoPickup = false;

        private const string ActionMapName = "Player";
        private const string PickupActionName = "PickupItem";

        private InputAction _pickupAction;
        private readonly HashSet<WorldItem> _itemsInZone = new HashSet<WorldItem>();
        private bool _promptShown;

        private void Awake()
        {
            if (_targetInventory == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "InventoryPickupZone : _targetInventory non assigné.", this);
                enabled = false;
                return;
            }

            var sphere = GetComponent<SphereCollider>();
            if (!sphere.isTrigger)
            {
                SurvainLog.Warn(SurvainLog.Category.Gameplay,
                    "InventoryPickupZone : SphereCollider doit être en mode IsTrigger pour détecter les WorldItem.",
                    this);
            }

            if (!_autoPickup)
            {
                if (_inputActions == null)
                {
                    SurvainLog.Error(SurvainLog.Category.Gameplay,
                        "InventoryPickupZone : _inputActions non assigné alors que _autoPickup=false.", this);
                    enabled = false;
                    return;
                }

                var map = _inputActions.FindActionMap(ActionMapName, throwIfNotFound: false);
                _pickupAction = map?.FindAction(PickupActionName, throwIfNotFound: false);
                if (_pickupAction == null)
                {
                    SurvainLog.Error(SurvainLog.Category.Gameplay,
                        $"InventoryPickupZone : action '{PickupActionName}' introuvable dans la map '{ActionMapName}'.",
                        this);
                    enabled = false;
                }
            }
        }

        private void OnEnable()
        {
            if (_pickupAction != null) _pickupAction.performed += OnPickupPerformed;
        }

        private void OnDisable()
        {
            if (_pickupAction != null) _pickupAction.performed -= OnPickupPerformed;
            HidePromptIfShown();
        }

        // ─── Détection ──────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            var worldItem = other.GetComponentInParent<WorldItem>();
            if (worldItem == null || !worldItem.IsConfigured) return;

            if (_autoPickup)
            {
                TryAbsorb(worldItem);
                return;
            }

            if (_itemsInZone.Add(worldItem))
            {
                worldItem.SetHighlighted(true);
                RefreshPrompt();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var worldItem = other.GetComponentInParent<WorldItem>();
            if (worldItem == null) return;

            if (_itemsInZone.Remove(worldItem))
            {
                worldItem.SetHighlighted(false);
                RefreshPrompt();
            }
        }

        // ─── Input handler (mode manuel) ────────────────────────────────────

        private void OnPickupPerformed(InputAction.CallbackContext _)
        {
            // Snapshot car TryAbsorb peut détruire le WorldItem (et donc retirer de la collection
            // via le Destroy → finalizer Unity ne notifie pas notre HashSet, on doit nettoyer après).
            var snapshot = new List<WorldItem>(_itemsInZone);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var item = snapshot[i];
                if (item == null) continue;
                TryAbsorb(item);
            }

            // Filtre les items détruits (techniquement encore non-null tant que la frame n'est pas finie)
            // ET les items vidés (Quantity tombée à 0 → seront destroyed bientôt).
            _itemsInZone.RemoveWhere(item => item == null || item.Quantity <= 0);
            RefreshPrompt();
        }

        // ─── Absorption ─────────────────────────────────────────────────────

        private void TryAbsorb(WorldItem worldItem)
        {
            if (worldItem == null || !worldItem.IsConfigured || worldItem.Quantity <= 0) return;

            var item = worldItem.Item;
            int available = worldItem.Quantity;

            int notAdded = _targetInventory.TryAdd(item, available);
            int absorbed = available - notAdded;
            if (absorbed <= 0) return;

            worldItem.Consume(absorbed);

            string remainingMsg = notAdded > 0
                ? $" — {notAdded} restant au sol (inventaire plein)"
                : string.Empty;
            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Pickup : {absorbed}x '{item.Id}' → inventaire{remainingMsg}.",
                this);
        }

        // ─── Prompt UI ──────────────────────────────────────────────────────

        private void RefreshPrompt()
        {
            if (_autoPickup) return;

            // Filtre les items détruits (techniquement encore non-null tant que la frame n'est pas finie)
            // ET les items vidés (Quantity tombée à 0 → seront destroyed bientôt).
            _itemsInZone.RemoveWhere(item => item == null || item.Quantity <= 0);

            if (_itemsInZone.Count == 0)
            {
                HidePromptIfShown();
                return;
            }

            // Choisit l'item le plus proche pour le prompt (cas où plusieurs items en zone).
            WorldItem nearest = null;
            float nearestSq = float.MaxValue;
            foreach (var item in _itemsInZone)
            {
                if (item == null) continue;
                float d = (item.transform.position - transform.position).sqrMagnitude;
                if (d < nearestSq)
                {
                    nearestSq = d;
                    nearest = item;
                }
            }

            if (nearest == null || nearest.Item == null)
            {
                HidePromptIfShown();
                return;
            }

            string label = nearest.Quantity > 1
                ? $"[F] Ramasser {nearest.Quantity}x {nearest.Item.DisplayName}"
                : $"[F] Ramasser {nearest.Item.DisplayName}";
            InteractionPrompt.Instance.Show(label);
            _promptShown = true;
        }

        private void HidePromptIfShown()
        {
            if (!_promptShown) return;
            _promptShown = false;
            if (InteractionPrompt.Instance != null) InteractionPrompt.Instance.Hide();
        }
    }
}
