#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Survain.Core;
using Survain.Items;

namespace Survain.Editor.Items
{
    /// <summary>
    /// Génère des icônes d'inventaire à partir du visuel 3D d'un équipement.
    ///
    /// Pour chaque ToolData du registry qui porte un HeldPrefab, on capture le rendu de
    /// preview d'Unity (AssetPreview — compatible URP, fond transparent), on l'écrit en PNG
    /// sous Assets/Art/Icons/{id}.png (importé en Sprite), puis on l'assigne à ItemData._icon.
    /// L'UI (InventorySlotView) affiche alors l'icône à la place du fallback couleur+texte.
    ///
    /// Les PNG générés sont des assets du projet (committables) : une fois créés, ils ne
    /// dépendent plus du pack source (gitignoré). Idempotent : relançable sans risque.
    ///
    /// Menu : Survain → Items → Generate Equipment Icons.
    /// </summary>
    public static class EquipmentIconGenerator
    {
        private const string IconsFolder = "Assets/Art/Icons";
        private const int MaxPollFrames = 600; // garde-fou anti-boucle infinie (~10 s à 60 fps)

        private static List<(ItemData item, GameObject prefab)> _queue;
        private static int _pollFrames;

        [MenuItem("Survain/Items/Generate Equipment Icons")]
        public static void GenerateEquipmentIcons()
        {
            if (_queue != null)
            {
                SurvainLog.Warn(SurvainLog.Category.System,
                    "EquipmentIconGenerator : génération déjà en cours, relance ignorée.");
                return;
            }

            var registry = LoadRegistry();
            if (registry == null)
            {
                SurvainLog.Error(SurvainLog.Category.System,
                    "EquipmentIconGenerator : aucun ItemRegistry trouvé dans le projet.");
                return;
            }

            _queue = new List<(ItemData, GameObject)>();
            foreach (var item in registry.AllItems)
            {
                if (item is ToolData tool && tool.HeldPrefab != null)
                    _queue.Add((item, tool.HeldPrefab));
            }

            if (_queue.Count == 0)
            {
                SurvainLog.Warn(SurvainLog.Category.System,
                    "EquipmentIconGenerator : aucun ToolData avec HeldPrefab assigné. " +
                    "Assigne d'abord un modèle 3D au champ 'Held Prefab' de tes outils.");
                return;
            }

            SurvainLog.Info(SurvainLog.Category.System,
                $"EquipmentIconGenerator : {_queue.Count} outil(s) avec HeldPrefab à traiter.");

            // Amorce la génération des previews (asynchrone côté Unity), puis on attend.
            foreach (var (_, prefab) in _queue) AssetPreview.GetAssetPreview(prefab);

            _pollFrames = 0;
            EditorApplication.update -= Poll; // garde-fou : jamais deux abonnements simultanés
            EditorApplication.update += Poll;
        }

        private static void Poll()
        {
            _pollFrames++;
            bool timedOut = _pollFrames > MaxPollFrames;
            if (!timedOut && AssetPreview.IsLoadingAssetPreviews()) return;

            EditorApplication.update -= Poll;
            if (timedOut)
                SurvainLog.Warn(SurvainLog.Category.System,
                    "EquipmentIconGenerator : délai de génération des previews dépassé, on traite ce qui est prêt.");

            FinalizeGeneration();
        }

        private static void FinalizeGeneration()
        {
            EnsureFolder(IconsFolder);

            // Passe 1 : rendu → PNG → import typé Sprite.
            var written = new List<(ItemData item, string relPath)>();
            foreach (var (item, prefab) in _queue)
            {
                var preview = AssetPreview.GetAssetPreview(prefab);
                if (preview == null)
                {
                    SurvainLog.Warn(SurvainLog.Category.System,
                        $"EquipmentIconGenerator : preview indisponible pour '{item.Id}', ignoré.", item);
                    continue;
                }

                var readable = CopyReadable(preview);
                string relPath = $"{IconsFolder}/{item.Id}.png";
                string absPath = Path.Combine(Application.dataPath, relPath.Substring("Assets/".Length));
                File.WriteAllBytes(absPath, readable.EncodeToPNG());
                Object.DestroyImmediate(readable);

                AssetDatabase.ImportAsset(relPath, ImportAssetOptions.ForceSynchronousImport);
                ConfigureAsSprite(relPath);
                written.Add((item, relPath));
            }

            // On laisse l'AssetDatabase finaliser les (ré)imports : sans ce Refresh, le Sprite
            // typé n'est pas encore chargeable dans la même passe (LoadAssetAtPath renvoie null).
            AssetDatabase.Refresh();

            // Passe 2 : charge le Sprite importé et l'assigne à l'item.
            int done = 0;
            foreach (var (item, relPath) in written)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(relPath);
                if (sprite == null)
                {
                    // Fallback : récupère le sous-asset Sprite directement.
                    foreach (var rep in AssetDatabase.LoadAllAssetRepresentationsAtPath(relPath))
                        if (rep is Sprite s) { sprite = s; break; }
                }

                if (sprite == null)
                {
                    SurvainLog.Warn(SurvainLog.Category.System,
                        $"EquipmentIconGenerator : Sprite non chargé pour '{item.Id}' ({relPath}). " +
                        "Vérifie que le PNG est bien importé en type Sprite.", item);
                    continue;
                }

                AssignIcon(item, sprite);
                SurvainLog.Info(SurvainLog.Category.System,
                    $"EquipmentIconGenerator : icône assignée à '{item.Id}'.", item);
                done++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SurvainLog.Info(SurvainLog.Category.System,
                $"EquipmentIconGenerator : {done}/{_queue.Count} icône(s) assignée(s) sous {IconsFolder}.");
            _queue = null;
        }

        // ─── Helpers ────────────────────────────────────────────────────────

        private static ItemRegistry LoadRegistry()
        {
            var guids = AssetDatabase.FindAssets("t:ItemRegistry");
            if (guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<ItemRegistry>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        // AssetPreview renvoie une texture non garantie « readable » selon les versions :
        // on la recopie via un RenderTexture pour pouvoir l'encoder en PNG (alpha préservé).
        private static Texture2D CopyReadable(Texture2D src)
        {
            int w = src.width, h = src.height;
            var rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32);
            var prev = RenderTexture.active;
            Graphics.Blit(src, rt);
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);
            return tex;
        }

        private static void ConfigureAsSprite(string relPath)
        {
            if (AssetImporter.GetAtPath(relPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                // Single (et non Multiple) : sans ça le PNG n'expose aucun sous-asset Sprite
                // → LoadAssetAtPath<Sprite> renvoie null et l'icône n'est jamais assignée.
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }
        }

        private static void AssignIcon(ItemData item, Sprite sprite)
        {
            var so = new SerializedObject(item);
            var prop = so.FindProperty("_icon");
            if (prop == null)
            {
                SurvainLog.Warn(SurvainLog.Category.System,
                    $"EquipmentIconGenerator : champ '_icon' introuvable sur '{item.Id}'.", item);
                return;
            }
            prop.objectReferenceValue = sprite;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(item);
        }

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder)) return;
            string parent = Path.GetDirectoryName(assetFolder).Replace('\\', '/');
            string leaf = Path.GetFileName(assetFolder);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif
