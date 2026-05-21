using UnityEngine;

namespace Survain.Items
{
    /// <summary>
    /// Définit le contenu et les règles d'un nœud de ressource (arbre, rocher, buisson...).
    /// Conteneur de données pur — la logique runtime (placement, hit detection, timer
    /// de récolte) appartiendra au composant ResourceNode (Survain.Gameplay.World)
    /// qui consommera ce SO. Issue #6.
    ///
    /// Split SO data ↔ MonoBehaviour runtime cohérent avec BiomeConfig ↔ TerrainGenerator.
    /// </summary>
    [CreateAssetMenu(
        fileName = "ResourceNodeData",
        menuName = "Survain/Items/Resource Node",
        order = 60)]
    public sealed class ResourceNodeData : ScriptableObject
    {
        [Header("Identité")]
        [Tooltip("Identifiant stable en kebab-case (ex: tree, rock, ore-deposit).")]
        [SerializeField] private string _id;

        [Tooltip("Nom affiché en UI et dans les logs.")]
        [SerializeField] private string _displayName;

        [Header("Récolte")]
        [Tooltip("Item produit par la récolte. Référence un ItemData (typiquement ResourceData).")]
        [SerializeField] private ItemData _producedItem;

        [Tooltip("Quantité produite par récolte complète.")]
        [Min(1)]
        [SerializeField] private int _producedQuantity = 1;

        [Tooltip("Nombre de coups d'outil nécessaires pour épuiser le nœud (gris/Basic). Plus = nœud plus dur.")]
        [Min(1)]
        [SerializeField] private int _hits = 3;

        [Tooltip("Temps de récolte de base en secondes. Modulé par ToolData.HarvestSpeed.")]
        [Range(0.1f, 30f)]
        [SerializeField] private float _harvestSeconds = 3f;

        [Tooltip("Famille d'outil requise. None = récolte à mains nues autorisée.")]
        [SerializeField] private ToolType _requiredTool = ToolType.None;

        [Header("Visuel")]
        [Tooltip("Prefab du visuel à instancier sur le nœud (mesh + matériaux). Optionnel : si null, un placeholder coloré est utilisé.")]
        [SerializeField] private GameObject _visualPrefab;

        [Header("Juice (feedback récolte)")]
        [Tooltip("Couleur des particules émises à chaque coup et à la destruction.")]
        [SerializeField] private Color _hitColor = new Color(0.6f, 0.4f, 0.2f);

        [Tooltip("Nombre de particules émises à chaque coup.")]
        [Range(0, 50)]
        [SerializeField] private int _hitParticleCount = 10;

        [Tooltip("Nombre de particules émises à la destruction (épuisement).")]
        [Range(0, 100)]
        [SerializeField] private int _depleteParticleCount = 30;

        [Tooltip("Échelle relative du visuel quand le nœud est à 1 HP (juste avant destruction). 1 = pas de réduction.")]
        [Range(0.3f, 1f)]
        [SerializeField] private float _minScaleAtLastHit = 0.6f;

        [Tooltip("Clip joué à chaque coup (et à la destruction, avec volume accentué). Nullable.")]
        [SerializeField] private AudioClip _hitSound;

        public string Id => _id;
        public string DisplayName => _displayName;
        public ItemData ProducedItem => _producedItem;
        public int ProducedQuantity => _producedQuantity;
        public int Hits => _hits;
        public float HarvestSeconds => _harvestSeconds;
        public ToolType RequiredTool => _requiredTool;
        public GameObject VisualPrefab => _visualPrefab;
        public Color HitColor => _hitColor;
        public int HitParticleCount => _hitParticleCount;
        public int DepleteParticleCount => _depleteParticleCount;
        public float MinScaleAtLastHit => _minScaleAtLastHit;
        public AudioClip HitSound => _hitSound;

        /// <summary>
        /// Vérifie si un outil donné permet de récolter ce nœud.
        /// Un nœud sans outil requis (None) accepte n'importe quoi, y compris null.
        /// </summary>
        public bool CanHarvestWith(ToolData tool)
        {
            if (_requiredTool == ToolType.None) return true;
            return tool != null && tool.ToolType == _requiredTool;
        }
    }
}
