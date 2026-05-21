using UnityEngine;
using Survain.Core;
using Survain.Items;

namespace Survain.Gameplay.Items
{
    /// <summary>
    /// Composant runtime d'un nœud de ressource posé dans le monde. Consomme un
    /// ResourceNodeData SO pour ses paramètres (HP, item produit, outil requis).
    ///
    /// Mécanique POC (D2 clic discret) : chaque appel à TryHit() avec l'outil
    /// approprié consomme 1 HP. À HP=0, le nœud spawne le drop et se désactive.
    /// Le respawn est traité en phase 3 (champ ResourceNodeData.respawnSeconds à venir).
    ///
    /// Le visuel est soit le prefab référencé par data.VisualPrefab, soit un
    /// placeholder coloré généré au Awake (cube vert pour arbres, sphère grise sinon).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ResourceNode : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("SO décrivant le type de nœud (item produit, outil requis, etc.).")]
        [SerializeField] private ResourceNodeData _data;

        [Tooltip("Prefab du drop spawné quand le nœud est épuisé. Si null, un cube placeholder est utilisé.")]
        [SerializeField] private GameObject _dropPrefab;

        [Header("Visuel placeholder (fallback si data.VisualPrefab est null)")]
        [Tooltip("Couleur de la primitive placeholder si aucun prefab visuel n'est fourni.")]
        [SerializeField] private Color _placeholderColor = new Color(0.3f, 0.6f, 0.2f);

        // État runtime
        private int _currentHits;
        private GameObject _visualInstance;

        public ResourceNodeData Data => _data;
        public int CurrentHits => _currentHits;
        public bool IsDepleted => _currentHits <= 0;
        public Transform VisualInstance => _visualInstance != null ? _visualInstance.transform : null;

        /// <summary>
        /// Émis à chaque coup réussi (avant la vérification d'épuisement).
        /// Consommé par ResourceNodeJuice pour le feedback visuel/audio.
        /// </summary>
        public event System.Action OnHit;

        /// <summary>
        /// Émis quand le nœud atteint 0 HP, juste avant la désactivation du GameObject.
        /// Les abonnés qui veulent survivre à la destruction (particules de fin)
        /// doivent spawner leurs effets dans des GameObjects standalone.
        /// </summary>
        public event System.Action OnDepleted;

        /// <summary>
        /// Assigne la data du nœud. Doit être appelé AVANT que le GameObject ne devienne actif
        /// (typiquement par un spawner qui crée le GO inactif, configure, puis active).
        /// </summary>
        public void SetData(ResourceNodeData data)
        {
            _data = data;
        }

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_data == null)
            {
                SurvainLog.Error(SurvainLog.Category.World,
                    "ResourceNode : data non assignée.", this);
                enabled = false;
                return;
            }

            _currentHits = _data.Hits;
            SpawnVisual();
        }

        // ─── API publique ───────────────────────────────────────────────────

        /// <summary>
        /// Tente de frapper le nœud avec l'outil donné. Renvoie true si le coup a porté
        /// (outil compatible et nœud encore actif), false sinon. Le caller (PlayerController)
        /// est responsable de l'animation/feedback associé.
        /// </summary>
        public bool TryHit(ToolData tool)
        {
            if (IsDepleted) return false;

            if (!_data.CanHarvestWith(tool))
            {
                SurvainLog.Info(SurvainLog.Category.Gameplay,
                    $"Outil incompatible pour le nœud '{_data.Id}' (requis : {_data.RequiredTool}).",
                    this);
                return false;
            }

            _currentHits--;
            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Coup porté sur '{_data.Id}' — HP restant : {_currentHits}/{_data.Hits}.", this);

            OnHit?.Invoke();

            if (_currentHits <= 0)
            {
                Deplete();
            }

            return true;
        }

        // ─── Internals ──────────────────────────────────────────────────────

        private void SpawnVisual()
        {
            if (_data.VisualPrefab != null)
            {
                _visualInstance = Instantiate(_data.VisualPrefab, transform);
                _visualInstance.transform.localPosition = Vector3.zero;
                _visualInstance.transform.localRotation = Quaternion.identity;
                return;
            }

            // Fallback : primitive colorée. Cube pour les "verticaux" (arbres), sphère sinon.
            bool isVertical = _data.RequiredTool == ToolType.Axe;
            var primitive = isVertical
                ? GameObject.CreatePrimitive(PrimitiveType.Cube)
                : GameObject.CreatePrimitive(PrimitiveType.Sphere);

            primitive.name = $"{_data.Id}_Placeholder";
            primitive.transform.SetParent(transform, false);

            if (isVertical)
            {
                primitive.transform.localScale = new Vector3(0.8f, 3f, 0.8f);
                primitive.transform.localPosition = Vector3.up * 1.5f;
            }
            else
            {
                primitive.transform.localScale = Vector3.one * 1.2f;
                primitive.transform.localPosition = Vector3.up * 0.5f;
            }

            var rend = primitive.GetComponent<Renderer>();
            if (rend != null && rend.sharedMaterial != null)
            {
                var mat = new Material(rend.sharedMaterial) { color = _placeholderColor };
                rend.sharedMaterial = mat;
            }

            _visualInstance = primitive;
        }

        private void Deplete()
        {
            SpawnDrop();
            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Nœud '{_data.Id}' épuisé.", this);
            OnDepleted?.Invoke();
            gameObject.SetActive(false);
        }

        private void SpawnDrop()
        {
            // Position de drop : légèrement au-dessus du sol pour que la gravité fasse le reste.
            Vector3 dropPos = transform.position + Vector3.up * 1f;

            if (_dropPrefab != null)
            {
                var drop = Instantiate(_dropPrefab, dropPos, Quaternion.identity);
                var placeholder = drop.GetComponent<WorldItemDropPlaceholder>();
                if (placeholder != null)
                {
                    placeholder.Configure(_data.ProducedItem, _data.ProducedQuantity);
                }
                return;
            }

            // Fallback : on génère un drop placeholder en code, sans prefab.
            var go = new GameObject($"Drop_{_data.ProducedItem?.Id ?? "unknown"}");
            go.transform.position = dropPos;
            var drop2 = go.AddComponent<WorldItemDropPlaceholder>();
            drop2.Configure(_data.ProducedItem, _data.ProducedQuantity);
        }
    }
}
