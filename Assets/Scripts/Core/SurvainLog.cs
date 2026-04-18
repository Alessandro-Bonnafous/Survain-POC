using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Survain.Core
{
    /// <summary>
    /// Wrapper centralisé autour de Debug.Log avec catégories et couleurs.
    /// Tous les logs du projet doivent passer par SurvainLog (jamais Debug.Log direct)
    /// pour garantir filtrage, formatage et stripping en build release.
    ///
    /// Usage :
    ///   SurvainLog.Info(SurvainLog.Category.Gameplay, "Joueur entre en zone PvE");
    ///   SurvainLog.Warn(SurvainLog.Category.System, "Config manquante", this);
    ///   SurvainLog.Error(SurvainLog.Category.Save, "Sauvegarde corrompue");
    ///
    /// Les Info/Warn sont strippés en build release (sans symbole SURVAIN_LOGS_ENABLED).
    /// Les Error sont TOUJOURS loggués, même en release.
    /// </summary>
    public static class SurvainLog
    {
        public enum Category
        {
            System = 0,
            Gameplay = 1,
            UI = 2,
            World = 3,
            AI = 4,
            Save = 5,
            Audio = 6,
            Network = 7,
        }

        // Couleurs Rich Text affichées dans la Console Unity
        private static readonly string[] CategoryColors =
        {
            "#9E9E9E", // System  — gris
            "#4CAF50", // Gameplay — vert
            "#2196F3", // UI       — bleu
            "#8BC34A", // World    — vert clair
            "#FF9800", // AI       — orange
            "#795548", // Save     — marron
            "#E91E63", // Audio    — rose
            "#9C27B0", // Network  — violet
        };

        /// <summary>
        /// Log d'information — strippé en build release.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("SURVAIN_LOGS_ENABLED")]
        public static void Info(Category category, string message, Object context = null)
        {
            Debug.Log(Format(category, message), context);
        }

        /// <summary>
        /// Log d'avertissement — strippé en build release.
        /// </summary>
        [Conditional("UNITY_EDITOR")]
        [Conditional("DEVELOPMENT_BUILD")]
        [Conditional("SURVAIN_LOGS_ENABLED")]
        public static void Warn(Category category, string message, Object context = null)
        {
            Debug.LogWarning(Format(category, message), context);
        }

        /// <summary>
        /// Log d'erreur — TOUJOURS actif, même en release.
        /// </summary>
        public static void Error(Category category, string message, Object context = null)
        {
            Debug.LogError(Format(category, message), context);
        }

        private static string Format(Category category, string message)
        {
            return $"<color={CategoryColors[(int)category]}><b>[{category}]</b></color> {message}";
        }
    }
}
