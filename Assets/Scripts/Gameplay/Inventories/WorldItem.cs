using UnityEngine;
using Survain.Core;
using Survain.Items;

namespace Survain.Gameplay.Inventories
{
    /// <summary>
    /// Item physique posé dans le monde (drop d'un nœud, drop manuel depuis l'inventaire).
    /// Tombe au sol via Rigidbody, peut être absorbé par un InventoryPickupZone.
    ///
    /// Remplace WorldItemDropPlaceholder (qui ne faisait que logguer). Le visuel reste
    /// un cube coloré au stade POC ; sera remplacé par le mesh propre de chaque item
    /// quand on aura des assets visuels par item (post-POC ou Sprint 5 polish).
    ///
    /// Cycle de vie typique :
    ///   ResourceNode.Deplete → Instantiate(WorldItem) + Configure(item, qty)
    ///   → WorldItem tombe au sol
    ///   → joueur s'approche → InventoryPickupZone.OnTriggerEnter
    ///     → tente TryAdd dans inventaire, appelle worldItem.Consume(absorbé)
    ///     → si Quantity restante == 0, WorldItem se Destroy
    ///     → sinon il reste au sol avec une quantité réduite
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WorldItem : MonoBehaviour
    {
        [Tooltip("Échelle du cube visuel.")]
        [SerializeField] private float _scale = 0.3f;

        [Tooltip("Couleur du cube fallback (utilisée si l'item n'a pas d'icône/material dédié).")]
        [SerializeField] private Color _fallbackColor = new Color(1f, 0.85f, 0.2f);

        [Tooltip("Force verticale appliquée au spawn pour faire 'sauter' le drop hors du nœud.")]
        [Range(0f, 10f)]
        [SerializeField] private float _spawnUpwardForce = 3f;

        [Header("Surbrillance")]
        [Tooltip("Couleur émissive appliquée quand l'item est survolé par la zone de pickup.")]
        [SerializeField] private Color _highlightEmission = new Color(1f, 0.85f, 0.4f) * 0.6f;

        private ItemData _item;
        private int _quantity;
        private bool _configured;
        private GameObject _visualCube;
        private Material _runtimeMaterial;
        private bool _highlighted;

        public ItemData Item => _item;
        public int Quantity => _quantity;
        public bool IsConfigured => _configured;

        /// <summary>
        /// Renseigne l'item et la quantité. À appeler juste après Instantiate.
        /// </summary>
        public void Configure(ItemData item, int quantity)
        {
            _item = item;
            _quantity = Mathf.Max(0, quantity);
            _configured = true;
            name = $"WorldItem_{(_item != null ? _item.Id : "null")}_x{_quantity}";
        }

        /// <summary>
        /// Active ou désactive la surbrillance (émissive URP Lit) sur le visuel.
        /// Appelé par InventoryPickupZone à l'entrée/sortie du WorldItem dans la zone.
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted) return;
            _highlighted = highlighted;
            if (_runtimeMaterial == null) return;

            if (highlighted)
            {
                _runtimeMaterial.EnableKeyword("_EMISSION");
                _runtimeMaterial.SetColor("_EmissionColor", _highlightEmission);
            }
            else
            {
                _runtimeMaterial.SetColor("_EmissionColor", Color.black);
                _runtimeMaterial.DisableKeyword("_EMISSION");
            }
        }

        /// <summary>
        /// Retire `amount` unités du stack au sol. Si la quantité tombe à 0, l'objet
        /// est détruit. Retourne la quantité effectivement consommée (clamp sur stock).
        /// </summary>
        public int Consume(int amount)
        {
            if (amount <= 0 || _quantity <= 0) return 0;
            int taken = Mathf.Min(amount, _quantity);
            _quantity -= taken;

            if (_quantity == 0)
            {
                Destroy(gameObject);
            }
            else
            {
                name = $"WorldItem_{_item.Id}_x{_quantity}";
            }
            return taken;
        }

        private void Start()
        {
            if (!_configured)
            {
                SurvainLog.Warn(SurvainLog.Category.Gameplay,
                    "WorldItem : Configure(item, qty) n'a pas été appelé après Instantiate.", this);
            }

            BuildVisual();
            ConfigureRigidbody();
            ConfigureCollider();
        }

        private void BuildVisual()
        {
            _visualCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _visualCube.name = "Visual";
            _visualCube.transform.SetParent(transform, false);
            _visualCube.transform.localScale = Vector3.one * _scale;

            // Le primitive cube apporte son propre BoxCollider — on le retire car la
            // physique est portée par le BoxCollider de la racine (RequireComponent).
            var primitiveCollider = _visualCube.GetComponent<Collider>();
            if (primitiveCollider != null) Destroy(primitiveCollider);

            var rend = _visualCube.GetComponent<Renderer>();
            if (rend != null)
            {
                // Shader URP Lit (CreatePrimitive utilise le shader Standard → rose en URP).
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                _runtimeMaterial = new Material(shader);
                _runtimeMaterial.SetColor("_BaseColor", _fallbackColor);
                _runtimeMaterial.color = _fallbackColor;
                rend.sharedMaterial = _runtimeMaterial;
            }
        }

        private void ConfigureRigidbody()
        {
            var rb = GetComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.AddForce(Vector3.up * _spawnUpwardForce, ForceMode.VelocityChange);
            rb.angularVelocity = new Vector3(
                Random.Range(-3f, 3f), Random.Range(-3f, 3f), Random.Range(-3f, 3f));
        }

        private void ConfigureCollider()
        {
            // BoxCollider porté par la racine ; on le cale sur l'échelle du visuel pour
            // que la physique de chute matche ce qu'on voit.
            var box = GetComponent<BoxCollider>();
            box.size = Vector3.one * _scale;
        }
    }
}
