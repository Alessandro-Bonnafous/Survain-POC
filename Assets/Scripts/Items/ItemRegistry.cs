using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using Survain.Core;

namespace Survain.Items
{
    /// <summary>
    /// Index global de tous les ItemData et ResourceNodeData du projet. Unique asset
    /// par convention (Assets/ScriptableObjects/Items/Registry.asset), référencé par
    /// GameSettings.ItemRegistry.
    ///
    /// Rôle :
    ///   - Résolution Id (string kebab-case) → asset, pour la sauvegarde et les recettes
    ///   - Validation en édition (format des Id, unicité, références manquantes)
    ///   - Itération typée par ItemType / par tier
    ///
    /// Peuplé manuellement (drag &amp; drop) au stade POC. À l'avenir on pourra
    /// le peupler automatiquement via un menu Editor (scan AssetDatabase).
    /// </summary>
    [CreateAssetMenu(
        fileName = "ItemRegistry",
        menuName = "Survain/Items/Registry",
        order = 70)]
    public sealed class ItemRegistry : ScriptableObject
    {
        // kebab-case : minuscules, chiffres, tirets ; pas de double tiret ; pas d'extrémité tirée.
        private static readonly Regex KebabCaseRegex = new Regex(
            "^[a-z0-9]+(-[a-z0-9]+)*$",
            RegexOptions.Compiled);

        [Header("Items")]
        [Tooltip("Tous les ItemData du projet. Renseigner manuellement.")]
        [SerializeField] private List<ItemData> _items = new List<ItemData>();

        [Header("Nœuds de ressources")]
        [Tooltip("Tous les ResourceNodeData du projet. Renseigner manuellement.")]
        [SerializeField] private List<ResourceNodeData> _resourceNodes = new List<ResourceNodeData>();

        private Dictionary<string, ItemData> _itemsById;
        private Dictionary<string, ResourceNodeData> _nodesById;

        /// <summary>
        /// Récupère un item par son Id. Null si introuvable.
        /// </summary>
        public ItemData GetItemById(string id)
        {
            EnsureLookups();
            return _itemsById.TryGetValue(id, out var item) ? item : null;
        }

        /// <summary>
        /// Récupère un nœud de ressource par son Id. Null si introuvable.
        /// </summary>
        public ResourceNodeData GetResourceNodeById(string id)
        {
            EnsureLookups();
            return _nodesById.TryGetValue(id, out var node) ? node : null;
        }

        /// <summary>
        /// Tous les items du registry (lecture seule, ne pas muter).
        /// </summary>
        public IReadOnlyList<ItemData> AllItems => _items;

        /// <summary>
        /// Tous les nœuds du registry (lecture seule, ne pas muter).
        /// </summary>
        public IReadOnlyList<ResourceNodeData> AllResourceNodes => _resourceNodes;

        private void EnsureLookups()
        {
            if (_itemsById != null && _nodesById != null) return;
            RebuildLookups();
        }

        private void RebuildLookups()
        {
            _itemsById = new Dictionary<string, ItemData>(_items.Count);
            foreach (var item in _items)
            {
                if (item == null || string.IsNullOrEmpty(item.Id)) continue;
                _itemsById[item.Id] = item;
            }

            _nodesById = new Dictionary<string, ResourceNodeData>(_resourceNodes.Count);
            foreach (var node in _resourceNodes)
            {
                if (node == null || string.IsNullOrEmpty(node.Id)) continue;
                _nodesById[node.Id] = node;
            }
        }

        private void OnEnable()
        {
            _itemsById = null;
            _nodesById = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ValidateItems();
            ValidateResourceNodes();
            RebuildLookups();
        }

        private void ValidateItems()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null)
                {
                    SurvainLog.Warn(SurvainLog.Category.System,
                        $"ItemRegistry : entrée Items[{i}] vide.", this);
                    continue;
                }

                var id = item.Id;
                if (string.IsNullOrWhiteSpace(id))
                {
                    SurvainLog.Warn(SurvainLog.Category.System,
                        $"ItemRegistry : '{item.name}' n'a pas d'Id renseigné.", item);
                    continue;
                }

                if (!KebabCaseRegex.IsMatch(id))
                {
                    SurvainLog.Warn(SurvainLog.Category.System,
                        $"ItemRegistry : Id '{id}' (asset '{item.name}') n'est pas en kebab-case (ex: stone-axe).",
                        item);
                }

                if (!seen.Add(id))
                {
                    SurvainLog.Error(SurvainLog.Category.System,
                        $"ItemRegistry : Id '{id}' en doublon (asset '{item.name}').", item);
                }
            }
        }

        private void ValidateResourceNodes()
        {
            var seen = new HashSet<string>();
            for (int i = 0; i < _resourceNodes.Count; i++)
            {
                var node = _resourceNodes[i];
                if (node == null)
                {
                    SurvainLog.Warn(SurvainLog.Category.System,
                        $"ItemRegistry : entrée ResourceNodes[{i}] vide.", this);
                    continue;
                }

                var id = node.Id;
                if (string.IsNullOrWhiteSpace(id))
                {
                    SurvainLog.Warn(SurvainLog.Category.System,
                        $"ItemRegistry : nœud '{node.name}' n'a pas d'Id renseigné.", node);
                    continue;
                }

                if (!KebabCaseRegex.IsMatch(id))
                {
                    SurvainLog.Warn(SurvainLog.Category.System,
                        $"ItemRegistry : Id '{id}' (nœud '{node.name}') n'est pas en kebab-case.", node);
                }

                if (!seen.Add(id))
                {
                    SurvainLog.Error(SurvainLog.Category.System,
                        $"ItemRegistry : Id '{id}' en doublon (nœud '{node.name}').", node);
                }

                if (node.ProducedItem == null)
                {
                    SurvainLog.Warn(SurvainLog.Category.System,
                        $"ItemRegistry : nœud '{node.name}' n'a pas de ProducedItem.", node);
                }
            }
        }
#endif
    }
}
