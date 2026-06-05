using System;

namespace Survain.Gameplay.World
{
    /// <summary>
    /// Point d'accès statique découplé à l'heure du monde, pour la logique gameplay (IA des PNJ,
    /// routines #15) qui ne doit dépendre ni du composant visuel <see cref="DayNightCycle"/> ni
    /// d'un FindObjectOfType. <see cref="DayNightCycle"/> reste l'autorité (rotation du soleil,
    /// ambient) et publie son état ici via <see cref="Publish"/>. Les consommateurs pollent
    /// <see cref="IsNight"/>/<see cref="Phase"/> ou s'abonnent à <see cref="OnPhaseChanged"/>.
    ///
    /// <see cref="HasClock"/> reste faux tant qu'aucun cycle n'a publié (scène sans jour/nuit) →
    /// les systèmes qui en dépendent restent neutres (pas de nuit forcée).
    /// </summary>
    public static class WorldClock
    {
        /// <summary>Vrai dès qu'un <see cref="DayNightCycle"/> a publié son état au moins une fois.</summary>
        public static bool HasClock { get; private set; }

        /// <summary>Heure normalisée courante : 0 = minuit, 0.5 = midi, 1 = minuit suivant.</summary>
        public static float Time01 { get; private set; }

        /// <summary>Phase courante du cycle (Day par défaut tant qu'aucune horloge n'a publié).</summary>
        public static DayPhase Phase { get; private set; } = DayPhase.Day;

        /// <summary>Nuit = phase où les PNJ rejoignent leur foyer pour se reposer (routines #15).</summary>
        public static bool IsNight => Phase == DayPhase.Night;

        /// <summary>Transition de phase (previousPhase, newPhase). Émis depuis <see cref="Publish"/>.</summary>
        public static event Action<DayPhase, DayPhase> OnPhaseChanged;

        /// <summary>
        /// Publie l'état courant (appelé chaque frame par <see cref="DayNightCycle"/>). Émet
        /// <see cref="OnPhaseChanged"/> uniquement au changement de phase.
        /// </summary>
        public static void Publish(float time01, DayPhase phase)
        {
            Time01 = time01;
            HasClock = true;

            if (phase != Phase)
            {
                DayPhase previous = Phase;
                Phase = phase;
                OnPhaseChanged?.Invoke(previous, phase);
            }
        }

        /// <summary>Réinitialise l'état statique (état + abonnés). Utile au rechargement de scène
        /// si le reload de domaine est désactivé (sinon les statiques repartent à zéro seuls).</summary>
        public static void Reset()
        {
            HasClock = false;
            Time01 = 0f;
            Phase = DayPhase.Day;
            OnPhaseChanged = null;
        }
    }
}
