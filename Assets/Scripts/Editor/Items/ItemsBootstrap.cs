#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using Survain.Core;
using Survain.Items;

namespace Survain.Editor.Items
{
    /// <summary>
    /// Outil Editor de bootstrap pour matérialiser les premiers items et nœuds de
    /// ressources demandés par l'issue #5 (Sprint 1) : 4 ressources brutes,
    /// 2 outils en pierre, 4 nœuds de récolte, et un ItemRegistry qui les agrège.
    ///
    /// Idempotent : si un asset existe déjà au chemin cible, il n'est pas écrasé.
    /// Permet de relancer la commande sans risque.
    ///
    /// Menu : Survain → Items → Bootstrap First Items.
    /// </summary>
    public static class ItemsBootstrap
    {
        private const string RootFolder = "Assets/ScriptableObjects/Items";
        private const string ResourcesFolder = RootFolder + "/Resources";
        private const string ToolsFolder = RootFolder + "/Tools";
        private const string NodesFolder = RootFolder + "/ResourceNodes";
        private const string RegistryPath = RootFolder + "/Registry.asset";

        [MenuItem("Survain/Items/Bootstrap First Items")]
        public static void BootstrapFirstItems()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(ResourcesFolder);
            EnsureFolder(ToolsFolder);
            EnsureFolder(NodesFolder);

            // --- Ressources (tier Basic, stackable) ---
            var rawWood = CreateOrGetResource(
                id: "raw-wood",
                displayName: "Bois brut",
                description: "Tronc grossier coupé à la hache. Matière première pour la construction et le craft.",
                maxStack: 99);

            var rawStone = CreateOrGetResource(
                id: "raw-stone",
                displayName: "Pierre brute",
                description: "Caillou taillé à la pioche. Solide, abondant, polyvalent.",
                maxStack: 99);

            var rawFibre = CreateOrGetResource(
                id: "raw-fibre",
                displayName: "Fibre végétale",
                description: "Tiges et lianes arrachées à mains nues. Utile pour cordages et premiers vêtements.",
                maxStack: 99);

            var rawOre = CreateOrGetResource(
                id: "raw-ore",
                displayName: "Minerai brut",
                description: "Roche minéralisée extraite à la pioche. À fondre pour en tirer du métal.",
                maxStack: 99);

            // --- Outils (tier Basic, non stackable) ---
            var stoneAxe = CreateOrGetTool(
                id: "stone-axe",
                displayName: "Hache en pierre",
                description: "Une lame de pierre attachée à un manche de bois. Permet d'abattre les arbres.",
                toolType: ToolType.Axe,
                harvestSpeed: 1f,
                maxDurability: 80);

            var stonePickaxe = CreateOrGetTool(
                id: "stone-pickaxe",
                displayName: "Pioche en pierre",
                description: "Une pointe de pierre durcie au feu. Permet de miner pierre et minerai.",
                toolType: ToolType.Pickaxe,
                harvestSpeed: 1f,
                maxDurability: 80);

            // --- Nœuds de ressources ---
            var treeNode = CreateOrGetNode(
                id: "tree",
                displayName: "Arbre",
                producedItem: rawWood,
                producedQuantity: 4,
                harvestSeconds: 4f,
                requiredTool: ToolType.Axe);

            var rockNode = CreateOrGetNode(
                id: "rock",
                displayName: "Rocher",
                producedItem: rawStone,
                producedQuantity: 3,
                harvestSeconds: 5f,
                requiredTool: ToolType.Pickaxe);

            var fibreNode = CreateOrGetNode(
                id: "fibre-bush",
                displayName: "Buisson fibreux",
                producedItem: rawFibre,
                producedQuantity: 2,
                harvestSeconds: 2f,
                requiredTool: ToolType.None);

            var oreNode = CreateOrGetNode(
                id: "ore-deposit",
                displayName: "Filon de minerai",
                producedItem: rawOre,
                producedQuantity: 2,
                harvestSeconds: 6f,
                requiredTool: ToolType.Pickaxe);

            // --- Registry global ---
            var registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(RegistryPath);
            if (registry == null)
            {
                registry = ScriptableObject.CreateInstance<ItemRegistry>();
                AssetDatabase.CreateAsset(registry, RegistryPath);
                SurvainLog.Info(SurvainLog.Category.System,
                    $"ItemsBootstrap : registry créé à {RegistryPath}.");
            }

            var so = new SerializedObject(registry);
            FillReferenceList(so, "_items", new ItemData[]
            {
                rawWood, rawStone, rawFibre, rawOre,
                stoneAxe, stonePickaxe,
            });
            FillReferenceList(so, "_resourceNodes", new ResourceNodeData[]
            {
                treeNode, rockNode, fibreNode, oreNode,
            });
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SurvainLog.Info(SurvainLog.Category.System,
                "ItemsBootstrap : 6 items + 4 nœuds créés ou rafraîchis, registry à jour.");

            EditorGUIUtility.PingObject(registry);
            Selection.activeObject = registry;
        }

        private static ResourceData CreateOrGetResource(string id, string displayName, string description, int maxStack)
        {
            var path = $"{ResourcesFolder}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ResourceData>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<ResourceData>();
            var so = new SerializedObject(asset);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_tier").enumValueIndex = (int)ItemTier.Basic;
            so.FindProperty("_isStackable").boolValue = true;
            so.FindProperty("_maxStackSize").intValue = maxStack;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(asset, path);
            SurvainLog.Info(SurvainLog.Category.System, $"ItemsBootstrap : créé {path}.");
            return asset;
        }

        private static ToolData CreateOrGetTool(
            string id, string displayName, string description,
            ToolType toolType, float harvestSpeed, int maxDurability)
        {
            var path = $"{ToolsFolder}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ToolData>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<ToolData>();
            var so = new SerializedObject(asset);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_tier").enumValueIndex = (int)ItemTier.Basic;
            so.FindProperty("_isStackable").boolValue = false;
            so.FindProperty("_maxStackSize").intValue = 1;
            so.FindProperty("_toolType").enumValueIndex = (int)toolType;
            so.FindProperty("_harvestSpeed").floatValue = harvestSpeed;
            so.FindProperty("_maxDurability").intValue = maxDurability;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(asset, path);
            SurvainLog.Info(SurvainLog.Category.System, $"ItemsBootstrap : créé {path}.");
            return asset;
        }

        private static ResourceNodeData CreateOrGetNode(
            string id, string displayName, ItemData producedItem,
            int producedQuantity, float harvestSeconds, ToolType requiredTool)
        {
            var path = $"{NodesFolder}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<ResourceNodeData>(path);
            if (existing != null)
            {
                // S'assure que le ProducedItem pointe bien sur l'asset attendu
                // (utile si l'asset cible vient juste d'être recréé).
                var soExisting = new SerializedObject(existing);
                soExisting.FindProperty("_producedItem").objectReferenceValue = producedItem;
                soExisting.ApplyModifiedPropertiesWithoutUndo();
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<ResourceNodeData>();
            var so = new SerializedObject(asset);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_producedItem").objectReferenceValue = producedItem;
            so.FindProperty("_producedQuantity").intValue = producedQuantity;
            so.FindProperty("_harvestSeconds").floatValue = harvestSeconds;
            so.FindProperty("_requiredTool").enumValueIndex = (int)requiredTool;
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(asset, path);
            SurvainLog.Info(SurvainLog.Category.System, $"ItemsBootstrap : créé {path}.");
            return asset;
        }

        private static void FillReferenceList(SerializedObject so, string propertyName, Object[] entries)
        {
            var list = so.FindProperty(propertyName);
            list.ClearArray();
            list.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            }
        }

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath)) return;

            var parent = Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var leaf = Path.GetFileName(assetPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf)) return;

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
