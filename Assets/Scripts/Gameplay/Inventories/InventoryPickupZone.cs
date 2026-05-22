using UnityEngine;
using Survain.Core;

namespace Survain.Gameplay.Inventories
{
    /// <summary>
    /// Zone trigger sphérique qui absorbe les WorldItem entrant en contact, vers l'inventaire référencé.
    ///
    /// Convention : composant à poser sur un enfant de _Player (ex: _Player/PickupZone/) avec
    /// un SphereCollider en mode IsTrigger=true. La racine _Player a déjà un CharacterController
    /// non-trigger, donc ne peut pas porter le trigger directement.
    ///
    /// Pickup auto activable/désactivable via _autoPickup (en attendant arbitrage Pascal sur
    /// auto vs manuel via touche E — cf. CLAUDE.md "Décisions en attente").
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SphereCollider))]
    public sealed class InventoryPickupZone : MonoBehaviour
    {
        [Tooltip("Inventaire cible (typiquement le Backpack du joueur).")]
        [SerializeField] private Inventory _targetInventory;

        [Tooltip("Active le pickup automatique au contact du WorldItem. " +
                 "Si false, l'objet doit être ramassé manuellement (logique à venir si Pascal arbitre vers manuel).")]
        [SerializeField] private bool _autoPickup = true;

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
                    "InventoryPickupZone : SphereCollider doit être en mode IsTrigger pour absorber les WorldItem.",
                    this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_autoPickup) return;
            TryAbsorb(other);
        }

        /// <summary>
        /// Si le collider porte (ou son parent porte) un WorldItem configuré, tente d'absorber
        /// sa quantité dans l'inventaire. Détruit le WorldItem si tout est rentré, sinon le
        /// laisse au sol avec une quantité réduite.
        /// </summary>
        private void TryAbsorb(Collider other)
        {
            var worldItem = other.GetComponentInParent<WorldItem>();
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
    }
}
