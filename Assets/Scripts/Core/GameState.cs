namespace Survain.Core
{
    /// <summary>
    /// États globaux du jeu. La transition entre états est gérée par GameManager.
    /// Pour le POC, 3 états suffisent. Étendre prudemment (cf. CLAUDE.md).
    /// </summary>
    public enum GameState
    {
        /// <summary>État initial. Menu principal, aucune simulation en cours.</summary>
        Menu = 0,

        /// <summary>Simulation active, joueur en jeu.</summary>
        Playing = 1,

        /// <summary>Simulation gelée (Time.timeScale = 0). Menu pause affiché.</summary>
        Paused = 2,
    }
}
