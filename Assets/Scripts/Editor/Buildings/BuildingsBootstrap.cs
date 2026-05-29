#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Survain.Core;
using Survain.Items;

namespace Survain.Editor.Buildings
{
    /// <summary>
    /// Outil Editor de bootstrap pour matérialiser les premiers bâtiments constructibles
    /// (issue #9, Sprint 2, modèle « chantier ») : des BuildingData placeholder de bâtiments
    /// ENTIERS (hutte, abri de stockage) avec leur coût en ressources, ajoutés au Registry.
    ///
    /// Ce sont des placeholders : sans prefab, le système génère un cube dimensionné par
    /// Size. Les vrais prefabs modulaires arriveront avec #10 ; il suffira alors de brancher
    /// le champ Prefab de chaque asset, sans toucher au reste du système.
    ///
    /// Idempotent : les assets existants ne sont pas écrasés, et l'ajout au Registry
    /// préserve les items déjà présents (n'utilise PAS le clear/refill de ItemsBootstrap).
    ///
    /// Menu : Survain → Items → Bootstrap Buildings.
    /// </summary>
    public static class BuildingsBootstrap
    {
        private const string RootFolder = "Assets/ScriptableObjects/Items";
        private const string BuildingsFolder = RootFolder + "/Buildings";
        private const string ResourcesFolder = RootFolder + "/Resources";
        private const string RegistryPath = RootFolder + "/Registry.asset";

        [MenuItem("Survain/Items/Bootstrap Buildings")]
        public static void BootstrapBuildings()
        {
            EnsureFolder(RootFolder);
            EnsureFolder(BuildingsFolder);

            var rawWood = LoadResource("raw-wood");
            var rawStone = LoadResource("raw-stone");
            if (rawWood == null || rawStone == null)
            {
                SurvainLog.Error(SurvainLog.Category.System,
                    "BuildingsBootstrap : raw-wood/raw-stone introuvables. Lance d'abord 'Survain → Items → Bootstrap First Items'.");
                return;
            }

            var hut = CreateOrGetBuilding(
                id: "building-hut",
                displayName: "Hutte",
                description: "Petit abri d'une pièce. Premier toit du colon.",
                category: BuildCategory.Shelter,
                size: new Vector3(3f, 2.5f, 3f),
                cost: new[] { (rawWood, 8), (rawStone, 4) });

            var shed = CreateOrGetBuilding(
                id: "building-shed",
                displayName: "Abri de stockage",
                description: "Petite remise en bois. Accueillera un coffre (Sprint 2 #10).",
                category: BuildCategory.Storage,
                size: new Vector3(2f, 2f, 2f),
                cost: new[] { (rawWood, 6) });

            AppendToRegistry(new ItemData[] { hut, shed });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SurvainLog.Info(SurvainLog.Category.System,
                "BuildingsBootstrap : 2 bâtiments créés ou rafraîchis, registry à jour.");

            var registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(RegistryPath);
            if (registry != null)
            {
                EditorGUIUtility.PingObject(registry);
                Selection.activeObject = registry;
            }
        }

        private static BuildingData CreateOrGetBuilding(
            string id, string displayName, string description,
            BuildCategory category, Vector3 size, (ResourceData item, int amount)[] cost)
        {
            var path = $"{BuildingsFolder}/{id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<BuildingData>();
            var so = new SerializedObject(asset);
            so.FindProperty("_id").stringValue = id;
            so.FindProperty("_displayName").stringValue = displayName;
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_tier").enumValueIndex = (int)ItemTier.Basic;
            so.FindProperty("_isStackable").boolValue = false;
            so.FindProperty("_maxStackSize").intValue = 1;
            so.FindProperty("_category").enumValueIndex = (int)category;
            so.FindProperty("_size").vector3Value = size;

            var costProp = so.FindProperty("_cost");
            costProp.arraySize = cost.Length;
            for (int i = 0; i < cost.Length; i++)
            {
                var element = costProp.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("_item").objectReferenceValue = cost[i].item;
                element.FindPropertyRelative("_amount").intValue = cost[i].amount;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(asset, path);
            SurvainLog.Info(SurvainLog.Category.System, $"BuildingsBootstrap : créé {path}.");
            return asset;
        }

        /// <summary>
        /// Ajoute les items au _items du Registry s'ils n'y sont pas déjà, en préservant
        /// les entrées existantes (contrairement au clear/refill de ItemsBootstrap).
        /// </summary>
        private static void AppendToRegistry(ItemData[] items)
        {
            var registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(RegistryPath);
            if (registry == null)
            {
                SurvainLog.Warn(SurvainLog.Category.System,
                    $"BuildingsBootstrap : Registry introuvable à {RegistryPath}. Structures créées mais non référencées.");
                return;
            }

            var so = new SerializedObject(registry);
            var list = so.FindProperty("_items");

            var present = new HashSet<Object>();
            for (int i = 0; i < list.arraySize; i++)
            {
                present.Add(list.GetArrayElementAtIndex(i).objectReferenceValue);
            }

            foreach (var item in items)
            {
                if (item == null || present.Contains(item)) continue;
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = item;
                present.Add(item);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
        }

        private static ResourceData LoadResource(string id)
        {
            return AssetDatabase.LoadAssetAtPath<ResourceData>($"{ResourcesFolder}/{id}.asset");
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
