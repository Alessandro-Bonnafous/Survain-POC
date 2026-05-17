using UnityEngine;
using UnityEngine.Serialization;
using Survain.Items;

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
        [FormerlySerializedAs("defaultBiome")]
        [SerializeField] private BiomeConfig _defaultBiome;

        [Tooltip("Seed pour la génération procédurale. 0 = aléatoire à chaque session.")]
        [FormerlySerializedAs("worldSeed")]
        [SerializeField] private int _worldSeed = 0;

        [Header("Items")]
        [Tooltip("Registry global référençant tous les ItemData et ResourceNodeData du projet.")]
        [SerializeField] private ItemRegistry _itemRegistry;

        [Header("Session")]
        [Tooltip("Active les logs verbeux supplémentaires hors Editor (DEVELOPMENT_BUILD).")]
        [FormerlySerializedAs("verboseLogging")]
        [SerializeField] private bool _verboseLogging = false;

        // Accesseurs publics — lecture seule.
        public BiomeConfig DefaultBiome => _defaultBiome;
        public int WorldSeed => _worldSeed;
        public ItemRegistry ItemRegistry => _itemRegistry;
        public bool VerboseLogging => _verboseLogging;
    }
}
