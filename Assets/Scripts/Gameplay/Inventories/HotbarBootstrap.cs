using UnityEngine;
using Survain.Core;
using Survain.Items;

namespace Survain.Gameplay.Inventories
{
    /// <summary>
    /// Outil de bootstrap qui pré-injecte une liste d'items dans un Inventory au démarrage.
    ///
    /// Utilisé en phase 1 du sprint #7 pour peupler la hotbar avec les outils de base
    /// (hache + pioche) en attendant l'UI inventaire (phase 2) qui permettra l'équipement
    /// par l'utilisateur depuis l'inventaire vers la hotbar.
    ///
    /// Tourne AVANT PlayerEquipment.Start (via DefaultExecutionOrder = -50) pour que
    /// l'outil du slot initial soit déjà présent au moment où PlayerEquipment.SetTool
    /// est appelé en Start.
    ///
    /// À supprimer en phase 3 (drop d'items depuis l'inventaire) ou à reconvertir en
    /// "save loader" quand on aura le système de sauvegarde.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-50)]
    public sealed class HotbarBootstrap : MonoBehaviour
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

        [Tooltip("Inventaire cible à pré-remplir au démarrage.")]
        [SerializeField] private Inventory _target;

        [Tooltip("Liste d'items à injecter dans l'ordre. L'ordre détermine les slots remplis.")]
        [SerializeField] private InitialEntry[] _entries;

        private void Start()
        {
            if (_target == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "HotbarBootstrap : _target non assigné.", this);
                return;
            }

            if (_entries == null || _entries.Length == 0)
            {
                SurvainLog.Warn(SurvainLog.Category.Gameplay,
                    "HotbarBootstrap : aucune entrée à injecter.", this);
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
                        $"HotbarBootstrap : {added}x '{entry.Item.Id}' injecté dans '{_target.name}'.",
                        this);
                }
                if (notAdded > 0)
                {
                    SurvainLog.Warn(SurvainLog.Category.Gameplay,
                        $"HotbarBootstrap : {notAdded}x '{entry.Item.Id}' non injecté (slots insuffisants).",
                        this);
                }
            }
        }
    }
}
