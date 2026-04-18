using UnityEngine;

namespace Survain.Data
{
    /// <summary>
    /// Réglages globaux du jeu (un seul asset par projet, par convention).
    /// Référence le BiomeConfig actif et expose les paramètres de session de base.
    ///
    /// Squelette POC. Tout ce qui touche au gameplay détaillé (stamina max,
    /// vitesses, courbes d'XP, etc.) viendra au fil des sprints.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameSettings",
        menuName = "Survain/Data/Game Settings",
        order = 0)]
    public sealed class GameSettings : ScriptableObject
    {
        [Header("Monde")]
        [Tooltip("Biome utilisé par défaut au démarrage du POC.")]
        [SerializeField] private BiomeConfig defaultBiome;

        [Tooltip("Seed pour la génération procédurale. 0 = aléatoire à chaque session.")]
        [SerializeField] private int worldSeed = 0;

        [Header("Session")]
        [Tooltip("Active les logs verbeux supplémentaires hors Editor (DEVELOPMENT_BUILD).")]
        [SerializeField] private bool verboseLogging = false;

        // Accesseurs publics — lecture seule.
        public BiomeConfig DefaultBiome => defaultBiome;
        public int WorldSeed => worldSeed;
        public bool VerboseLogging => verboseLogging;
    }
}
