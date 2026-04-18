using System;
using UnityEngine;
using UnityEngine.Events;

namespace Survain.Core
{
    /// <summary>
    /// Singleton persistant qui détient l'état global du jeu.
    /// Doit être présent dans la scène de bootstrap via prefab — l'auto-création est interdite.
    ///
    /// Transitions valides :
    ///   Menu    → Playing
    ///   Playing ⇄ Paused
    ///   Paused  → Menu (quitter vers le menu depuis la pause)
    ///   Playing → Menu (fin de partie / quit to menu)
    ///
    /// Toute autre transition est refusée (log d'erreur + état inchangé).
    ///
    /// Notification : à chaque transition réussie, deux signaux sont émis :
    ///   - OnStateChanged (event C# statique) — pour le code
    ///   - StateChanged   (UnityEvent sérialisé) — pour les listeners inspector
    /// Les deux sont déclenchés APRÈS que CurrentState ait été mis à jour.
    /// Signature : (previousState, newState).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameManager : MonoBehaviour
    {
        // ─── Singleton ──────────────────────────────────────────────────────

        public static GameManager Instance { get; private set; }

        // ─── État ───────────────────────────────────────────────────────────

        [SerializeField] private GameState initialState = GameState.Menu;

        public GameState CurrentState { get; private set; }

        // ─── Notifications ──────────────────────────────────────────────────

        /// <summary>Event C# statique. (previousState, newState).</summary>
        public static event Action<GameState, GameState> OnStateChanged;

        [Serializable] public class GameStateChangedEvent : UnityEvent<GameState, GameState> { }

        [SerializeField] private GameStateChangedEvent stateChanged = new GameStateChangedEvent();
        public GameStateChangedEvent StateChanged => stateChanged;

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                SurvainLog.Warn(SurvainLog.Category.System,
                    $"GameManager dupliqué détecté sur '{gameObject.name}'. Destruction de ce doublon.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            CurrentState = initialState;
            ApplyTimeScaleFor(CurrentState);
            SurvainLog.Info(SurvainLog.Category.System, $"GameManager initialisé en état {CurrentState}.", this);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ─── API publique ───────────────────────────────────────────────────

        /// <summary>
        /// Tente une transition vers <paramref name="next"/>.
        /// Retourne true si la transition a eu lieu, false sinon.
        /// </summary>
        public bool TryChangeState(GameState next)
        {
            if (next == CurrentState)
            {
                SurvainLog.Warn(SurvainLog.Category.System,
                    $"Tentative de transition vers l'état courant ({next}) — ignorée.", this);
                return false;
            }

            if (!IsTransitionAllowed(CurrentState, next))
            {
                SurvainLog.Error(SurvainLog.Category.System,
                    $"Transition refusée : {CurrentState} → {next}.", this);
                return false;
            }

            GameState previous = CurrentState;
            CurrentState = next;
            ApplyTimeScaleFor(next);

            SurvainLog.Info(SurvainLog.Category.System, $"Transition d'état : {previous} → {next}.", this);

            OnStateChanged?.Invoke(previous, next);
            stateChanged?.Invoke(previous, next);

            return true;
        }

        // Raccourcis sémantiques — à privilégier dans le code appelant.
        public bool StartGame()    => TryChangeState(GameState.Playing);
        public bool PauseGame()    => TryChangeState(GameState.Paused);
        public bool ResumeGame()   => TryChangeState(GameState.Playing);
        public bool ReturnToMenu() => TryChangeState(GameState.Menu);

        // ─── Logique de transition ──────────────────────────────────────────

        private static bool IsTransitionAllowed(GameState from, GameState to)
        {
            switch (from)
            {
                case GameState.Menu:
                    return to == GameState.Playing;

                case GameState.Playing:
                    return to == GameState.Paused || to == GameState.Menu;

                case GameState.Paused:
                    return to == GameState.Playing || to == GameState.Menu;

                default:
                    return false;
            }
        }

        private static void ApplyTimeScaleFor(GameState state)
        {
            // Le GameManager ne touche au timeScale QUE pour l'état Paused.
            // Toute autre logique de timeScale (slow-mo, etc.) viendra ailleurs.
            Time.timeScale = state == GameState.Paused ? 0f : 1f;
        }
    }
}
