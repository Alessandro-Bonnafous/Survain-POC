using UnityEngine;

namespace Survain.Data
{
    /// <summary>
    /// Paramètres du cycle jour/nuit : durée, courbes d'intensité, gradients de couleur.
    /// Conteneur de données pur — aucune logique ici.
    /// Consommé par DayNightCycle (Survain.Gameplay.World).
    /// </summary>
    [CreateAssetMenu(
        fileName = "TimeOfDayConfig",
        menuName = "Survain/Data/Time Of Day Config",
        order = 40)]
    public sealed class TimeOfDayConfig : ScriptableObject
    {
        [Header("Temps")]
        [Tooltip("Durée réelle d'un cycle complet en secondes. 600 = 10 min temps réel.")]
        [Range(30f, 7200f)]
        [SerializeField] private float _cycleDurationSeconds = 600f;

        [Tooltip("Heure de départ normalisée. 0 = minuit, 0.25 = aube, 0.5 = midi, 0.75 = crépuscule.")]
        [Range(0f, 1f)]
        [SerializeField] private float _startTime01 = 0.3f;

        [Header("Soleil")]
        [Tooltip("Yaw du soleil (degrés autour de l'axe Y). Modifie l'orientation Est-Ouest et donne une teinte aux ombres.")]
        [Range(0f, 360f)]
        [SerializeField] private float _northYawDeg = 170f;

        [Tooltip("Couleur du soleil selon l'heure normalisée [0..1]. 0 = minuit, 0.5 = midi.")]
        [SerializeField] private Gradient _sunColor = CreateDefaultSunGradient();

        [Tooltip("Intensité du soleil selon l'heure normalisée [0..1].")]
        [SerializeField] private AnimationCurve _sunIntensity = CreateDefaultIntensityCurve();

        [Header("Ambient")]
        [Tooltip("Couleur de la lumière ambiante (mode Flat) selon l'heure normalisée [0..1].")]
        [SerializeField] private Gradient _ambientColor = CreateDefaultAmbientGradient();

        public float CycleDurationSeconds => _cycleDurationSeconds;
        public float StartTime01 => _startTime01;
        public float NorthYawDeg => _northYawDeg;
        public Gradient SunColor => _sunColor;
        public AnimationCurve SunIntensity => _sunIntensity;
        public Gradient AmbientColor => _ambientColor;

        // ─── Défauts ────────────────────────────────────────────────────────

        private static Gradient CreateDefaultSunGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.15f, 0.20f, 0.35f), 0.00f), // minuit — bleu nuit
                    new GradientColorKey(new Color(0.30f, 0.25f, 0.30f), 0.20f), // pré-aube
                    new GradientColorKey(new Color(1.00f, 0.60f, 0.30f), 0.25f), // aube — orange
                    new GradientColorKey(new Color(1.00f, 0.85f, 0.60f), 0.30f), // matin — jaune chaud
                    new GradientColorKey(new Color(1.00f, 0.97f, 0.90f), 0.50f), // midi — blanc
                    new GradientColorKey(new Color(1.00f, 0.85f, 0.60f), 0.70f), // après-midi
                    new GradientColorKey(new Color(1.00f, 0.55f, 0.30f), 0.75f), // crépuscule — orange
                    new GradientColorKey(new Color(0.30f, 0.25f, 0.30f), 0.80f), // post-crépuscule
                    new GradientColorKey(new Color(0.15f, 0.20f, 0.35f), 1.00f), // minuit (boucle)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });
            return g;
        }

        private static AnimationCurve CreateDefaultIntensityCurve()
        {
            return new AnimationCurve(
                new Keyframe(0.00f, 0.05f),
                new Keyframe(0.20f, 0.10f),
                new Keyframe(0.25f, 0.50f),
                new Keyframe(0.30f, 1.00f),
                new Keyframe(0.70f, 1.00f),
                new Keyframe(0.75f, 0.50f),
                new Keyframe(0.80f, 0.10f),
                new Keyframe(1.00f, 0.05f));
        }

        private static Gradient CreateDefaultAmbientGradient()
        {
            var g = new Gradient();
            g.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.05f, 0.07f, 0.15f), 0.00f), // minuit — très sombre
                    new GradientColorKey(new Color(0.15f, 0.13f, 0.20f), 0.22f), // pré-aube
                    new GradientColorKey(new Color(0.45f, 0.40f, 0.40f), 0.30f), // matin
                    new GradientColorKey(new Color(0.60f, 0.62f, 0.65f), 0.50f), // midi — gris bleuté clair
                    new GradientColorKey(new Color(0.45f, 0.40f, 0.40f), 0.70f), // après-midi
                    new GradientColorKey(new Color(0.20f, 0.15f, 0.20f), 0.78f), // crépuscule
                    new GradientColorKey(new Color(0.05f, 0.07f, 0.15f), 1.00f), // minuit (boucle)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f),
                });
            return g;
        }
    }
}
