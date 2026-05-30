using UnityEngine;
using Survain.Core;
using Survain.Gameplay.Interaction;
using Survain.Gameplay.Inventories;
using Survain.UI;

namespace Survain.Gameplay.Buildings
{
    /// <summary>
    /// Coffre de stockage : un bâtiment fonctionnel (#10) qui porte un Inventory secondaire,
    /// persistant le temps de la session. Ouvrable par le joueur via la touche d'interaction
    /// générique (E) — implémente <see cref="IInteractable"/>.
    ///
    /// Posé en code à la complétion d'un chantier (ConstructionSite.Complete) quand la
    /// BuildingData a une StorageCapacity > 0. L'Inventory est créé en runtime et configuré
    /// à la capacité voulue (cf. Inventory.ConfigureCapacity).
    ///
    /// Phase 1a : l'interaction logge l'état (pas d'UI). Le panneau container côte à côte
    /// arrive en phase 1b ; il se branchera sur <see cref="Inventory"/> via InventorySlotView.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StorageContainer : MonoBehaviour, IInteractable
    {
        [Tooltip("Couleur émissive appliquée quand le joueur vise le coffre.")]
        [SerializeField] private Color _highlightEmission = new Color(1f, 0.9f, 0.5f) * 0.5f;

        private Inventory _inventory;
        private string _label = "Coffre";
        private Renderer[] _renderers;
        private bool _highlighted;

        public Inventory Inventory => _inventory;
        public string Label => _label;

        /// <summary>
        /// Crée et configure l'Inventory du coffre. Appelé juste après l'ajout du composant
        /// (la BuildingVisualFactory a déjà créé le visuel enfant, d'où le cache des renderers).
        /// </summary>
        public void Initialize(int capacity, string label)
        {
            if (!string.IsNullOrEmpty(label)) _label = label;

            _inventory = gameObject.AddComponent<Inventory>();
            _inventory.ConfigureCapacity(capacity);

            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        }

        // ─── IInteractable ──────────────────────────────────────────────────

        public bool IsInteractable => true;

        public string GetInteractionPrompt() => $"[E] Ouvrir {_label}";

        public void Interact(Inventory actorInventory)
        {
            if (ContainerUI.Instance != null)
            {
                ContainerUI.Instance.Open(_inventory, _label);
            }
            else
            {
                SurvainLog.Warn(SurvainLog.Category.Gameplay,
                    $"Coffre '{_label}' : ContainerUI absent de la scène, impossible d'ouvrir le panneau.", this);
            }
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted || _renderers == null) return;
            _highlighted = highlighted;

            Color emission = highlighted ? _highlightEmission : Color.black;
            for (int i = 0; i < _renderers.Length; i++)
            {
                var rend = _renderers[i];
                if (rend == null) continue;
                var mat = rend.material; // clone auto, instance par renderer (cf. convention ResourceNode)
                if (mat == null || !mat.HasProperty("_EmissionColor")) continue;
                if (highlighted) mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
        }
    }
}
