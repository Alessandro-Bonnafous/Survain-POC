using System;
using UnityEngine;
using UnityEngine.Rendering;
using Survain.Core;
using Survain.Data;

namespace Survain.Gameplay.World
{
    /// <summary>
    /// Phases d'un cycle jour/nuit. Bornes :
    /// Night = [0..0.2) ∪ [0.8..1) — Dawn = [0.2..0.3) — Day = [0.3..0.7) — Dusk = [0.7..0.8).
    /// </summary>
    public enum DayPhase
    {
        Night = 0,
        Dawn  = 1,
        Day   = 2,
        Dusk  = 3,
    }

    /// <summary>
    /// Fait tourner une Directional Light pour simuler le soleil, module sa couleur et son intensité,
    /// et pilote l'ambient (mode Flat). Expose l'heure courante [0..1] et la phase courante.
    ///
    /// Conventions :
    /// - 0 = minuit, 0.25 = aube, 0.5 = midi, 0.75 = crépuscule, 1 = minuit suivant (boucle).
    /// - Rotation du soleil : `Quaternion.Euler(t01 * 360 - 90, NorthYaw, 0)`. À midi (t01=0.5),
    ///   xRot = 90° → la lumière pointe vers le bas (soleil au zénith).
    /// - L'ambient est forcé en mode Flat au Start (écrase la config skybox de la scène) ; les
    ///   couleurs viennent du gradient du SO.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DayNightCycle : MonoBehaviour
    {
        // ─── Configuration ──────────────────────────────────────────────────

        [Header("Configuration")]
        [SerializeField] private TimeOfDayConfig _config;

        [Tooltip("Directional Light qui représente le soleil. Sa rotation, couleur et intensité sont pilotées.")]
        [SerializeField] private Light _sunLight;

        [Header("Démarrage")]
        [Tooltip("Si vrai, le cycle avance automatiquement. Si faux, l'heure reste figée sur CurrentTime01.")]
        [SerializeField] private bool _autoAdvance = true;

        // ─── État runtime ───────────────────────────────────────────────────

        private float _currentTime01;
        private DayPhase _currentPhase;

        // ─── API publique ───────────────────────────────────────────────────

        /// <summary>Heure normalisée courante : 0 = minuit, 0.5 = midi, 1 = minuit suivant.</summary>
        public float CurrentTime01 => _currentTime01;

        /// <summary>Phase courante du cycle.</summary>
        public DayPhase CurrentPhase => _currentPhase;

        /// <summary>Événement émis à chaque transition de phase. (previousPhase, newPhase).</summary>
        public event Action<DayPhase, DayPhase> OnPhaseChanged;

        /// <summary>Permet à un système externe (debug, save) de positionner l'heure directement.</summary>
        public void SetTime01(float t01)
        {
            _currentTime01 = Mathf.Repeat(t01, 1f);
            ApplyVisualState(_currentTime01);
            UpdatePhase();
            WorldClock.Publish(_currentTime01, _currentPhase);
        }

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_config == null)
            {
                SurvainLog.Error(SurvainLog.Category.World,
                    "DayNightCycle : config non assignée.", this);
                enabled = false;
                return;
            }

            if (_sunLight == null)
            {
                SurvainLog.Error(SurvainLog.Category.World,
                    "DayNightCycle : sunLight non assignée.", this);
                enabled = false;
                return;
            }

            _currentTime01 = Mathf.Repeat(_config.StartTime01, 1f);
            _currentPhase = ComputePhase(_currentTime01);

            // Publie l'état initial pour que la logique gameplay (routines PNJ #15) lise une heure
            // valide dès le premier Update, avant même que ce cycle ait avancé.
            WorldClock.Publish(_currentTime01, _currentPhase);
        }

        private void Start()
        {
            // On force le mode Flat pour que notre gradient ambient s'applique réellement.
            // Le mode par défaut "Skybox" ignorerait RenderSettings.ambientLight.
            RenderSettings.ambientMode = AmbientMode.Flat;
            ApplyVisualState(_currentTime01);
        }

        private void Update()
        {
            if (!_autoAdvance) return;

            float dt = Time.deltaTime / _config.CycleDurationSeconds;
            _currentTime01 = Mathf.Repeat(_currentTime01 + dt, 1f);

            ApplyVisualState(_currentTime01);
            UpdatePhase();
            WorldClock.Publish(_currentTime01, _currentPhase);
        }

        // ─── Visuel ─────────────────────────────────────────────────────────

        private void ApplyVisualState(float t01)
        {
            // Rotation : à t01 = 0.5 (midi), xRot = 90° → lumière vers le bas.
            float xRot = t01 * 360f - 90f;
            _sunLight.transform.rotation = Quaternion.Euler(xRot, _config.NorthYawDeg, 0f);

            _sunLight.color = _config.SunColor.Evaluate(t01);
            _sunLight.intensity = _config.SunIntensity.Evaluate(t01);

            RenderSettings.ambientLight = _config.AmbientColor.Evaluate(t01);
        }

        // ─── Phase ──────────────────────────────────────────────────────────

        private void UpdatePhase()
        {
            DayPhase next = ComputePhase(_currentTime01);
            if (next == _currentPhase) return;

            DayPhase previous = _currentPhase;
            _currentPhase = next;

            SurvainLog.Info(SurvainLog.Category.World,
                $"Transition de phase : {previous} → {next} (t01={_currentTime01:F3}).", this);

            OnPhaseChanged?.Invoke(previous, next);
        }

        private static DayPhase ComputePhase(float t01)
        {
            if (t01 < 0.2f || t01 >= 0.8f) return DayPhase.Night;
            if (t01 < 0.3f) return DayPhase.Dawn;
            if (t01 < 0.7f) return DayPhase.Day;
            return DayPhase.Dusk;
        }

        // ─── Debug ──────────────────────────────────────────────────────────

        [ContextMenu("Force time → Midnight (0.0)")]
        private void DebugSetMidnight() => SetTime01(0.0f);

        [ContextMenu("Force time → Dawn (0.25)")]
        private void DebugSetDawn() => SetTime01(0.25f);

        [ContextMenu("Force time → Noon (0.5)")]
        private void DebugSetNoon() => SetTime01(0.5f);

        [ContextMenu("Force time → Dusk (0.75)")]
        private void DebugSetDusk() => SetTime01(0.75f);
    }
}
