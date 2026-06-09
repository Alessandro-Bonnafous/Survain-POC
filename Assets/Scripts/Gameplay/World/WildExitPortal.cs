using UnityEngine;
using Survain.Gameplay.Interaction;
using Survain.Gameplay.Inventories;

namespace Survain.Gameplay.World
{
    /// <summary>
    /// Portail de sortie (#74) : posé dans l'instance sauvage (typiquement au point d'entrée),
    /// il ramène le joueur au village via la touche d'interaction (E) — implémente
    /// <see cref="IInteractable"/>, délègue à <see cref="WildInstanceManager.ExitWild"/>.
    ///
    /// Construit son visuel (totem lumineux) + son collider d'interaction en code : il suffit de
    /// poser un GameObject vide à l'entrée, d'y ajouter ce composant et d'assigner le manager.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WildExitPortal : MonoBehaviour, IInteractable
    {
        private static readonly Color PortalColor = new Color(0.55f, 0.85f, 1f);
        private static readonly Color HighlightEmission = new Color(0.7f, 0.9f, 1f) * 0.6f;

        [Tooltip("Manager de l'instance sauvage (sur _WildZone).")]
        [SerializeField] private WildInstanceManager _instance;

        private Renderer[] _renderers;
        private bool _highlighted;

        private void Awake()
        {
            BuildVisual();
            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
        }

        private void BuildVisual()
        {
            // Totem cylindrique : son CapsuleCollider sert de cible au raycast d'interaction.
            var totem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            totem.name = "PortalTotem";
            totem.transform.SetParent(transform, false);
            totem.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            totem.transform.localScale = new Vector3(0.7f, 1.5f, 0.7f); // ~3 m de haut

            var rend = totem.GetComponent<Renderer>();
            if (rend != null)
            {
                var mat = rend.material; // clone auto
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", PortalColor);
                mat.color = PortalColor;
            }

            var lightGo = new GameObject("PortalLight");
            lightGo.transform.SetParent(transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2f, 0f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = PortalColor;
            light.range = 10f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.None;
        }

        public bool IsInteractable => _instance != null;

        public string GetInteractionPrompt() => "[E] Retourner au village";

        public void Interact(Inventory actorInventory)
        {
            if (_instance != null) _instance.ExitWild();
        }

        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted || _renderers == null) return;
            _highlighted = highlighted;

            Color emission = highlighted ? HighlightEmission : Color.black;
            for (int i = 0; i < _renderers.Length; i++)
            {
                var rend = _renderers[i];
                if (rend == null) continue;
                var mat = rend.material;
                if (mat == null || !mat.HasProperty("_EmissionColor")) continue;
                if (highlighted) mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
        }
    }
}
