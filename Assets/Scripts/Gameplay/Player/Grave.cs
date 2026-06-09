using UnityEngine;
using Survain.Core;
using Survain.Gameplay.Interaction;
using Survain.Gameplay.Inventories;
using Survain.UI;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Tombe du joueur (#19) : conteneur lootable autonome où est déversé tout le stuff à la mort.
    /// Ouvrable via la touche d'interaction générique (E) — implémente <see cref="IInteractable"/>,
    /// réutilise <see cref="ContainerUI"/> (drag tombe ↔ sac, comme un coffre). Le loot disparaît
    /// après un timer (arbitrage : punitif mais récupérable) : à expiration, la tombe se détruit.
    ///
    /// Construite en code (visuel placeholder : tertre + stèle, collider pour le raycast). Pas un
    /// Building : artefact de mort, namespace Survain.Gameplay.Player. Le marqueur sur la map
    /// (visibilité à distance) arrive en phase 3.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Grave : MonoBehaviour, IInteractable
    {
        private static readonly Color MoundColor = new Color(0.28f, 0.20f, 0.13f);
        private static readonly Color StoneColor = new Color(0.45f, 0.45f, 0.48f);
        private static readonly Color HighlightEmission = new Color(0.7f, 0.85f, 1f) * 0.5f;

        private const string Label = "Tombe";

        private Inventory _inventory;
        private Renderer[] _renderers;
        private bool _highlighted;
        private bool _despawnScheduled;
        private float _despawnAt;

        public Inventory Inventory => _inventory;

        /// <summary>Crée une tombe à la position donnée, avec un inventaire de la capacité voulue
        /// et un timer de disparition (≤ 0 = permanente). Le stuff est ajouté ensuite par l'appelant
        /// (via <see cref="Inventory"/>).</summary>
        public static Grave Create(Vector3 position, int capacity, float despawnSeconds)
        {
            var go = new GameObject("Grave");
            go.transform.position = position;
            var grave = go.AddComponent<Grave>();
            grave.Build(capacity, despawnSeconds);
            return grave;
        }

        private void Build(int capacity, float despawnSeconds)
        {
            _inventory = gameObject.AddComponent<Inventory>();
            _inventory.ConfigureCapacity(capacity);

            BuildVisual();
            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

            if (despawnSeconds > 0f)
            {
                _despawnScheduled = true;
                _despawnAt = Time.time + despawnSeconds;
            }
        }

        private void BuildVisual()
        {
            // Tertre (boîte aplatie) — porte aussi un collider pour le raycast d'interaction.
            var mound = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mound.name = "Mound";
            mound.transform.SetParent(transform, false);
            mound.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            mound.transform.localScale = new Vector3(1.1f, 0.3f, 0.8f);
            Tint(mound, MoundColor);

            // Stèle (dalle verticale).
            var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = "Headstone";
            stone.transform.SetParent(transform, false);
            stone.transform.localPosition = new Vector3(0f, 0.55f, -0.3f);
            stone.transform.localScale = new Vector3(0.6f, 0.7f, 0.12f);
            Tint(stone, StoneColor);
        }

        private static void Tint(GameObject go, Color color)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend == null) return;
            var mat = rend.material; // clone auto (convention runtime, jamais sharedMaterial)
            if (mat == null) return;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
        }

        private void Update()
        {
            if (_despawnScheduled && Time.time >= _despawnAt)
            {
                SurvainLog.Info(SurvainLog.Category.Gameplay, "Tombe expirée : le loot est perdu.", this);
                Destroy(gameObject);
            }
        }

        // ─── IInteractable ──────────────────────────────────────────────────

        public bool IsInteractable => true;

        public string GetInteractionPrompt() => $"[E] Fouiller la {Label}";

        public void Interact(Inventory actorInventory)
        {
            if (ContainerUI.Instance != null)
            {
                ContainerUI.Instance.Open(_inventory, Label);
            }
            else
            {
                SurvainLog.Warn(SurvainLog.Category.Gameplay,
                    "Tombe : ContainerUI absent de la scène, impossible d'ouvrir le panneau.", this);
            }
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
