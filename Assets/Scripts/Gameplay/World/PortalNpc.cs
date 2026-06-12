using UnityEngine;
using Survain.Gameplay.Interaction;
using Survain.Gameplay.Inventories;

namespace Survain.Gameplay.World
{
    /// <summary>
    /// PNJ « portail / éclaireur » (#74) : posé au village, il fait entrer le joueur dans
    /// l'instance sauvage via la touche d'interaction (E) — implémente <see cref="IInteractable"/>,
    /// délègue à <see cref="WildInstanceManager.EnterWild"/> (qui régénère puis téléporte, ou
    /// préserve l'instance si une tombe y est).
    ///
    /// À poser sur un GameObject doté d'un collider (placeholder ou avatar PNJ). Le raycast du
    /// PlayerInteractor remonte au composant via GetComponentInParent.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PortalNpc : MonoBehaviour, IInteractable
    {
        [Tooltip("Manager de l'instance sauvage (sur _WildZone).")]
        [SerializeField] private WildInstanceManager _instance;

        [Tooltip("Couleur émissive au survol.")]
        [SerializeField] private Color _highlightEmission = new Color(0.5f, 0.8f, 1f) * 0.5f;

        [Header("Pose au sol")]
        [Tooltip("Colle le PNJ sur le terrain village (généré au runtime) au démarrage.")]
        [SerializeField] private bool _snapToGround = true;

        [Tooltip("Décalage vertical après le snap. 0 = pivot aux pieds (avatar) ; ~1 = pivot au centre (capsule placeholder).")]
        [SerializeField] private float _groundOffset = 1f;

        private Renderer[] _renderers;
        private bool _highlighted;

        private void Awake() => _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

        private void Start()
        {
            // Le terrain village est généré au runtime → recoller le PNJ au sol (sinon il reste à
            // son Y d'éditeur, typiquement sous la map). Order par défaut (0) > TerrainGenerator (-100).
            if (_snapToGround) WildInstanceManager.SnapToGround(transform, _groundOffset);
        }

        public bool IsInteractable => _instance != null;

        public string GetInteractionPrompt() => "[E] Partir en zone sauvage";

        public void Interact(Inventory actorInventory)
        {
            if (_instance != null) _instance.EnterWild();
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
                var mat = rend.material; // clone auto (jamais sharedMaterial)
                if (mat == null || !mat.HasProperty("_EmissionColor")) continue;
                if (highlighted) mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
        }
    }
}
