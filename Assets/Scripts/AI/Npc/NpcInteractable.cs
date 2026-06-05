using UnityEngine;
using Survain.Gameplay.Interaction;
using Survain.Gameplay.Inventories;
using Survain.UI;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Porté par le contremaître (seul PNJ interactable). Via l'interaction générique (touche E),
    /// ouvre/ferme le panneau de gestion du village (NpcManagementPanel : roster + assignation
    /// des métiers) — #14 phase 3. Implémente IInteractable, donc réutilise le PlayerInteractor
    /// existant (raycast caméra + prompt + surbrillance) sans interacteur dédié.
    ///
    /// Note : E sera aussi la touche du futur dialogue PNJ (Sprint 3+) ; à la convergence, E
    /// ouvrira un menu PNJ (Gérer / Parler) ou l'une des actions basculera sur une autre touche.
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

        public string GetInteractionPrompt() => "[E] Gérer le village";

        public void Interact(Inventory actorInventory)
        {
            // actorInventory = sac du joueur (enfant de _Player) → sert de cible « regard » au contremaître.
            Transform player = actorInventory != null ? actorInventory.transform : null;
            NpcManagementPanel.Instance.Toggle(_controller, player);
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
