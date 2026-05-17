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

        [Tooltip("Temps de récolte de base en secondes. Modulé par ToolData.HarvestSpeed.")]
        [Range(0.1f, 30f)]
        [SerializeField] private float _harvestSeconds = 3f;

        [Tooltip("Famille d'outil requise. None = récolte à mains nues autorisée.")]
        [SerializeField] private ToolType _requiredTool = ToolType.None;

        public string Id => _id;
        public string DisplayName => _displayName;
        public ItemData ProducedItem => _producedItem;
        public int ProducedQuantity => _producedQuantity;
        public float HarvestSeconds => _harvestSeconds;
        public ToolType RequiredTool => _requiredTool;

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
