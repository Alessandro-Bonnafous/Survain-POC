using UnityEngine;
using Survain.Core;
using Survain.Items;

namespace Survain.Gameplay.Inventories
{
    /// <summary>
    /// Pré-injecte une liste d'items dans un Inventory au démarrage.
    ///
    /// Successeur de HotbarBootstrap (phase 1) — renommé en phase 3 car la cible n'est
    /// plus la hotbar mais le backpack : l'utilisateur peut désormais équiper depuis
    /// l'inventaire vers la hotbar via drag & drop. La hotbar démarre vide, le joueur
    /// équipe ses outils initiaux en glissant depuis le backpack.
    ///
    /// Tourne AVANT PlayerEquipment.Start (via DefaultExecutionOrder = -50) pour que
    /// les items soient présents au moment où PlayerEquipment lit la hotbar (cas où
    /// quelqu'un voudrait quand même pointer cet InventoryBootstrap vers la hotbar).
    ///
    /// À supprimer à terme quand on aura le système de sauvegarde — il fera le job de
    /// peupler l'inventaire au load.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class InventoryBootstrap : MonoBehaviour
    {
        [System.Serializable]
        public struct InitialEntry
        {
            [Tooltip("Item à injecter dans l'inventaire.")]
            public ItemData Item;

            [Tooltip("Quantité à ajouter. Sera répartie selon le MaxStackSize de l'item.")]
            [Min(1)]
            public int Quantity;
        }

        [Tooltip("Inventaire cible à pré-remplir au démarrage (typiquement le Backpack).")]
        [SerializeField] private Inventory _target;

        [Tooltip("Liste d'items à injecter dans l'ordre. L'ordre détermine les slots remplis.")]
        [SerializeField] private InitialEntry[] _entries;

        private void Start()
        {
            if (_target == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "InventoryBootstrap : _target non assigné.", this);
                return;
            }

            if (_entries == null || _entries.Length == 0)
            {
                SurvainLog.Warn(SurvainLog.Category.Gameplay,
                    "InventoryBootstrap : aucune entrée à injecter.", this);
                return;
            }

            for (int i = 0; i < _entries.Length; i++)
            {
                var entry = _entries[i];
                if (entry.Item == null || entry.Quantity <= 0) continue;

                int notAdded = _target.TryAdd(entry.Item, entry.Quantity);
                int added = entry.Quantity - notAdded;

                if (added > 0)
                {
                    SurvainLog.Info(SurvainLog.Category.Gameplay,
                        $"InventoryBootstrap : {added}x '{entry.Item.Id}' injecté dans '{_target.name}'.",
                        this);
                }
                if (notAdded > 0)
                {
                    SurvainLog.Warn(SurvainLog.Category.Gameplay,
                        $"InventoryBootstrap : {notAdded}x '{entry.Item.Id}' non injecté (slots insuffisants).",
                        this);
                }
            }
        }
    }
}
