using UnityEngine;
using Survain.Core;
using Survain.Items;

namespace Survain.Gameplay.Items
{
    /// <summary>
    /// Drop placeholder phase 1 : un cube qui tombe au sol via un Rigidbody,
    /// log sa configuration à l'impact. Sera remplacé par un vrai prefab WorldItem
    /// (mesh + pickup via inventaire) au sprint #7.
    ///
    /// Configure(item, quantité) doit être appelé juste après l'instanciation, sinon
    /// le drop est inerte et log un warning au Start.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WorldItemDropPlaceholder : MonoBehaviour
    {
        [Tooltip("Échelle du cube placeholder.")]
        [SerializeField] private float _scale = 0.3f;

        [Tooltip("Couleur du cube placeholder.")]
        [SerializeField] private Color _color = new Color(1f, 0.85f, 0.2f);

        [Tooltip("Force verticale appliquée au spawn pour faire 'sauter' le drop hors du nœud.")]
        [Range(0f, 10f)]
        [SerializeField] private float _spawnUpwardForce = 3f;

        private ItemData _item;
        private int _quantity;
        private bool _configured;
        private bool _impactLogged;

        /// <summary>
        /// Renseigne l'item et la quantité représentés par ce drop. À appeler après Instantiate.
        /// </summary>
        public void Configure(ItemData item, int quantity)
        {
            _item = item;
            _quantity = quantity;
            _configured = true;
            name = $"Drop_{(_item != null ? _item.Id : "null")}_x{_quantity}";
        }

        private void Start()
        {
            if (!_configured)
            {
                SurvainLog.Warn(SurvainLog.Category.Gameplay,
                    "WorldItemDropPlaceholder : Configure(item, qty) n'a pas été appelé.", this);
            }

            BuildVisual();
        }

        private void BuildVisual()
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Cube";
            cube.transform.SetParent(transform, false);
            cube.transform.localScale = Vector3.one * _scale;

            var rend = cube.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                var mat = new Material(rend.sharedMaterial) { color = _color };
                rend.sharedMaterial = mat;
            }

            var rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 0.5f;
            rb.AddForce(Vector3.up * _spawnUpwardForce, ForceMode.VelocityChange);

            // Petit spin aléatoire pour donner du juice gratuit.
            rb.angularVelocity = new Vector3(
                Random.Range(-3f, 3f), Random.Range(-3f, 3f), Random.Range(-3f, 3f));
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_impactLogged) return;
            _impactLogged = true;

            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Drop posé au sol : {_quantity}x '{(_item != null ? _item.Id : "null")}' " +
                $"sur '{collision.gameObject.name}'. (Pickup à venir en #7.)",
                this);
        }
    }
}
