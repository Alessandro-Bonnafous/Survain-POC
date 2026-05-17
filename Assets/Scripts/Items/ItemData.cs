using UnityEngine;

namespace Survain.Items
{
    /// <summary>
    /// Base abstraite de tout item du jeu. Conteneur de données pur — aucune logique
    /// de gameplay. Les sous-classes (ResourceData, ToolData, WeaponData, ArmorData,
    /// BuildingData, ConsumableData) portent les champs spécifiques à leur rôle.
    ///
    /// Identité stable : le champ Id (string kebab-case) sert de clé persistante
    /// pour la sauvegarde, les recettes de craft et la résolution via ItemRegistry.
    /// Il est validé en édition (format + unicité) par ItemRegistry.OnValidate.
    ///
    /// L'ItemType exposé sur la base permet un filtrage rapide sans cast vers la
    /// sous-classe concrète. Chaque sous-classe surcharge Type avec sa valeur fixe.
    /// </summary>
    public abstract class ItemData : ScriptableObject
    {
        [Header("Identité")]
        [Tooltip("Identifiant stable en kebab-case (ex: stone-axe). Sert de clé pour la save et le registry.")]
        [SerializeField] private string _id;

        [Tooltip("Nom affiché en UI et dans les logs.")]
        [SerializeField] private string _displayName;

        [Tooltip("Description longue (tooltip inventaire, fiche d'item).")]
        [TextArea(2, 5)]
        [SerializeField] private string _description;

        [Tooltip("Icône d'inventaire. Peut rester vide au stade POC.")]
        [SerializeField] private Sprite _icon;

        [Header("Classification")]
        [Tooltip("Tier de qualité (basique, sauvage, supérieur).")]
        [SerializeField] private ItemTier _tier = ItemTier.Basic;

        [Header("Stack")]
        [Tooltip("Si l'item est stackable dans l'inventaire (typiquement ressources oui, outils/armes/armures non).")]
        [SerializeField] private bool _isStackable = false;

        [Tooltip("Taille max d'un stack. Ignoré si non stackable.")]
        [Min(1)]
        [SerializeField] private int _maxStackSize = 1;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public ItemTier Tier => _tier;
        public bool IsStackable => _isStackable;
        public int MaxStackSize => _isStackable ? _maxStackSize : 1;

        /// <summary>
        /// Rôle métier de l'item. Surchargé en valeur fixe par chaque sous-classe concrète.
        /// </summary>
        public abstract ItemType Type { get; }
    }
}
