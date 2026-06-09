using UnityEngine;
using Survain.Core;
using Survain.Gameplay.Interaction;
using Survain.Gameplay.Inventories;

namespace Survain.Gameplay.Buildings
{
    /// <summary>
    /// Point de repos du joueur (#19) : ajouté à la complétion d'un chantier dont la BuildingData
    /// a <see cref="Items.BuildingData.ProvidesRespawn"/> (le lit). Le joueur l'« active » via la
    /// touche d'interaction (E) — implémente <see cref="IInteractable"/> — pour en faire son point
    /// de réapparition « maison » ; le respawn y revient toujours, quel que soit le lieu de mort.
    ///
    /// Un seul lit actif à la fois (<see cref="Active"/>, statique). Consommé par PlayerDeath.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RespawnPoint : MonoBehaviour, IInteractable
    {
        private static readonly Color HighlightEmission = new Color(0.5f, 0.8f, 1f) * 0.5f;
        private static readonly Color ActiveEmission = new Color(0.4f, 0.7f, 1f) * 0.55f;

        /// <summary>Lit actuellement défini comme point de repos (null si aucun).</summary>
        public static RespawnPoint Active { get; private set; }

        private Renderer[] _renderers;
        private bool _highlighted;

        public bool IsActive => Active == this;

        /// <summary>Position de réapparition : légèrement devant le lit pour ne pas spawn dedans.</summary>
        public Vector3 RespawnPosition => transform.position + transform.forward * 1.2f + Vector3.up * 0.1f;

        private void Awake() => _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

        private void OnDestroy()
        {
            if (Active == this) Active = null;
        }

        // ─── IInteractable ──────────────────────────────────────────────────

        public bool IsInteractable => true;

        public string GetInteractionPrompt() =>
            IsActive ? "Point de repos actif" : "[E] Définir comme point de repos";

        public void Interact(Inventory actorInventory)
        {
            if (IsActive) return;
            Active = this;
            SurvainLog.Info(SurvainLog.Category.Gameplay, "Point de repos défini sur ce lit.", this);
            ApplyEmission(); // glow « actif » persistant
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted) return;
            _highlighted = highlighted;
            ApplyEmission();
        }

        /// <summary>Émissive = surbrillance au survol, sinon glow discret si actif, sinon éteint.</summary>
        private void ApplyEmission()
        {
            if (_renderers == null) return;
            Color emission = _highlighted ? HighlightEmission : (IsActive ? ActiveEmission : Color.black);
            for (int i = 0; i < _renderers.Length; i++)
            {
                var rend = _renderers[i];
                if (rend == null) continue;
                var mat = rend.material; // clone auto (jamais sharedMaterial)
                if (mat == null || !mat.HasProperty("_EmissionColor")) continue;
                if (emission != Color.black) mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
        }
    }
}
