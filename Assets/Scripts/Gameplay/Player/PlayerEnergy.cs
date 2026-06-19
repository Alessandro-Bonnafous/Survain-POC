using System;
using UnityEngine;
using Survain.Core;
using Survain.Data;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Énergie du joueur (combat #16, Phase A / A1) : réserve unique (spec : 100 pts), régénération
    /// après délai, barre HUD (<see cref="Survain.UI.PlayerEnergyBar"/>). Composant satellite sur
    /// _Player, distinct de PlayerController — calqué trait pour trait sur <see cref="PlayerHealth"/>.
    ///
    /// A1 ne livre que la <b>réserve + la barre</b> : la consommation réelle (course, esquive,
    /// auto-attack, compétences) arrive en A2/A3. La classe est <b>neutre vis-à-vis de Q1</b>
    /// (réserve partagée) : c'est l'usage en aval qui décidera de puiser dans cette même jauge.
    ///
    /// Instance statique exposée (comme PlayerHealth.Instance) pour que les consommateurs accèdent
    /// à l'énergie sans FindObjectOfType.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerEnergy : MonoBehaviour
    {
        /// <summary>Instance unique en scène (set OnEnable). Consommée par les systèmes de combat/mobilité.</summary>
        public static PlayerEnergy Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Stats d'énergie (réserve max, régén).")]
        [SerializeField] private PlayerEnergyConfig _config;

        // ─── État runtime ───────────────────────────────────────────────────

        private float _maxEnergy;
        private float _lastConsumeTime;

        public float CurrentEnergy { get; private set; }
        public float MaxEnergy => _maxEnergy;

        /// <summary>Énergie normalisée [0..1] (pour la barre / overlays).</summary>
        public float Normalized => _maxEnergy > 0f ? CurrentEnergy / _maxEnergy : 0f;

        // ─── Events ─────────────────────────────────────────────────────────

        /// <summary>(current, max) — émis à toute variation d'énergie (conso, restauration, régén).</summary>
        public event Action<float, float> EnergyChanged;

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_config == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerEnergy : config non assignée.", this);
                enabled = false;
                return;
            }

            _maxEnergy = Mathf.Max(1f, _config.MaxEnergy);
            CurrentEnergy = _maxEnergy;
        }

        private void OnEnable() => Instance = this;
        private void OnDisable() { if (Instance == this) Instance = null; }

        private void Start() => EnergyChanged?.Invoke(CurrentEnergy, _maxEnergy);

        // ─── API publique ───────────────────────────────────────────────────

        /// <summary>Tente de consommer <paramref name="amount"/> points d'énergie. Échoue (renvoie
        /// <c>false</c>) si la réserve est insuffisante — <b>pas de consommation partielle</b>.
        /// Un montant ≤ 0 est un no-op réussi.</summary>
        public bool TryConsume(float amount)
        {
            if (amount <= 0f) return true;
            if (CurrentEnergy < amount) return false;

            CurrentEnergy -= amount;
            _lastConsumeTime = Time.time;
            EnergyChanged?.Invoke(CurrentEnergy, _maxEnergy);
            return true;
        }

        /// <summary>Restaure de l'énergie (clampée à MaxEnergy). Sans effet si déjà au max.</summary>
        public void Restore(float amount)
        {
            if (amount <= 0f || CurrentEnergy >= _maxEnergy) return;
            CurrentEnergy = Mathf.Min(_maxEnergy, CurrentEnergy + amount);
            EnergyChanged?.Invoke(CurrentEnergy, _maxEnergy);
        }

        // ─── DEBUG ──────────────────────────────────────────────────────────

        /// <summary>Valide la DoD d'A1 sans toucher à l'input ni à la scène. À retirer en A2 quand
        /// la vraie consommation (auto-attack/esquive/course) arrivera.</summary>
        [ContextMenu("DEBUG — Consommer 25")]
        private void DebugConsume25()
        {
            bool ok = TryConsume(25f);
            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"DEBUG conso 25 énergie : {(ok ? "ok" : "insuffisant")} — {CurrentEnergy:0}/{_maxEnergy:0}.", this);
        }

        // ─── Interne ────────────────────────────────────────────────────────

        private void Update()
        {
            // Régénération après un délai sans consommation.
            float regen = _config.RegenPerSecond;
            if (regen > 0f && CurrentEnergy < _maxEnergy
                && Time.time >= _lastConsumeTime + _config.RegenDelaySeconds)
            {
                Restore(regen * Time.deltaTime);
            }
        }
    }
}
