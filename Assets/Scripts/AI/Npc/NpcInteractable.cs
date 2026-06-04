using UnityEngine;
using Survain.Gameplay.Interaction;
using Survain.Gameplay.Inventories;
using Survain.UI;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Rend un PNJ examinable via l'interaction générique (touche E) : ouvre/ferme son panneau
    /// de détail (jauges de besoins) — #13 phase 3. Implémente IInteractable, donc réutilise le
    /// PlayerInteractor existant (raycast caméra + prompt + surbrillance) sans interacteur dédié.
    ///
    /// Note : E sera aussi la touche du futur dialogue PNJ (Sprint 3+) ; à la convergence, E
    /// ouvrira un menu PNJ (Examiner / Parler) ou l'examen basculera sur une autre touche.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcInteractable : MonoBehaviour, IInteractable
    {
        [Tooltip("Couleur émissive quand le joueur vise le PNJ.")]
        [SerializeField] private Color _highlightEmission = new Color(0.4f, 0.7f, 1f) * 0.5f;

        private NpcController _controller;
        private Renderer[] _renderers;
        private bool _cached;
        private bool _highlighted;

        public bool IsInteractable => _controller != null;

        private void Awake() => _controller = GetComponent<NpcController>();

        public string GetInteractionPrompt()
        {
            string n = _controller != null && _controller.Data != null ? _controller.Data.DisplayName : "PNJ";
            return $"[E] Examiner {n}";
        }

        public void Interact(Inventory actorInventory)
        {
            if (_controller != null) NpcDetailPanel.Instance.Toggle(_controller);
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted) return;
            _highlighted = highlighted;
            EnsureCache();

            Color emission = highlighted ? _highlightEmission : Color.black;
            for (int i = 0; i < _renderers.Length; i++)
            {
                var rend = _renderers[i];
                if (rend == null) continue;
                var mat = rend.material; // clone par renderer (Synty partage le sharedMaterial)
                if (mat == null || !mat.HasProperty("_EmissionColor")) continue;
                if (highlighted) mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
        }

        private void EnsureCache()
        {
            if (_cached) return;
            _cached = true;
            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        }
    }
}
