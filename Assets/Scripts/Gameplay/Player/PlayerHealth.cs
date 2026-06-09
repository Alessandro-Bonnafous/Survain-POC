using System;
using UnityEngine;
using Survain.Core;
using Survain.Data;

namespace Survain.Gameplay.Player
{
    /// <summary>
    /// Vie du joueur (#19) : HP, encaissement de dégâts, régénération, mort. Composant satellite
    /// sur _Player, distinct de PlayerController (qui garde son scope locomotion). Source de dégâts
    /// POC : l'attaque des ennemis (EnemyAttackState, #17), branchée en fin de telegraph.
    ///
    /// Instance statique exposée (comme PlayerController.Instance) pour que les sources de dégâts
    /// ciblent le joueur sans FindObjectOfType. La séquence de mort réelle (écran, drop du stuff en
    /// tombe, respawn) est portée par <see cref="PlayerDeath"/>, abonné à l'event <see cref="Died"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerHealth : MonoBehaviour
    {
        /// <summary>Instance unique en scène (set OnEnable). Consommée par les sources de dégâts.</summary>
        public static PlayerHealth Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Stats de vie (PV max, régén, invulnérabilité).")]
        [SerializeField] private PlayerHealthConfig _config;

        // ─── État runtime ───────────────────────────────────────────────────

        private int _maxHp;
        private float _lastDamageTime;
        private float _regenAccumulator;

        public int CurrentHp { get; private set; }
        public int MaxHp => _maxHp;
        public bool IsDead { get; private set; }

        /// <summary>Vie normalisée [0..1] (pour les barres / overlays).</summary>
        public float Normalized => _maxHp > 0 ? (float)CurrentHp / _maxHp : 0f;

        // ─── Events ─────────────────────────────────────────────────────────

        /// <summary>(current, max) — émis à toute variation de PV (dégât, soin, reset).</summary>
        public event Action<int, int> HealthChanged;

        /// <summary>Émis une fois quand les PV tombent à 0. La séquence de mort (phase 2) s'y abonne.</summary>
        public event Action Died;

        /// <summary>Émis quand le joueur revient à la vie (revive / respawn).</summary>
        public event Action Revived;

        // ─── Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_config == null)
            {
                SurvainLog.Error(SurvainLog.Category.Gameplay,
                    "PlayerHealth : config non assignée.", this);
                enabled = false;
                return;
            }

            _maxHp = Mathf.Max(1, _config.MaxHp);
            CurrentHp = _maxHp;
        }

        private void OnEnable() => Instance = this;
        private void OnDisable() { if (Instance == this) Instance = null; }

        private void Start() => HealthChanged?.Invoke(CurrentHp, _maxHp);

        // ─── API publique ───────────────────────────────────────────────────

        /// <summary>Inflige des dégâts (ignorés si déjà mort, montant ≤ 0, ou pendant la fenêtre
        /// d'invulnérabilité post-coup). À 0 PV : mort.</summary>
        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;
            if (Time.time < _lastDamageTime + _config.InvulnerabilitySeconds) return;

            _lastDamageTime = Time.time;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            HealthChanged?.Invoke(CurrentHp, _maxHp);
            SurvainLog.Info(SurvainLog.Category.Gameplay, $"Joueur touché : {CurrentHp}/{_maxHp} PV.", this);

            if (CurrentHp == 0) Die();
        }

        /// <summary>Soigne (clampé à MaxHp). Sans effet si mort.</summary>
        public void Heal(int amount)
        {
            if (IsDead || amount <= 0 || CurrentHp >= _maxHp) return;
            CurrentHp = Mathf.Min(_maxHp, CurrentHp + amount);
            HealthChanged?.Invoke(CurrentHp, _maxHp);
        }

        /// <summary>Remet la vie au maximum et ré-active le joueur (revive / respawn). Phase 2
        /// l'appellera après le placement au point de respawn.</summary>
        public void ResetToFull()
        {
            CurrentHp = _maxHp;
            _regenAccumulator = 0f;
            if (IsDead)
            {
                IsDead = false;
                Revived?.Invoke();
            }
            HealthChanged?.Invoke(CurrentHp, _maxHp);
        }

        /// <summary>Tue le joueur immédiatement (debug / test).</summary>
        [ContextMenu("DEBUG / Tuer le joueur")]
        public void Kill()
        {
            if (IsDead) return;
            CurrentHp = 0;
            HealthChanged?.Invoke(CurrentHp, _maxHp);
            Die();
        }

        // ─── Interne ────────────────────────────────────────────────────────

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;
            SurvainLog.Info(SurvainLog.Category.Gameplay, "Joueur mort.", this);
            Died?.Invoke();
        }

        private void Update()
        {
            if (IsDead) return;

            // Régénération après un délai sans dégât.
            float regen = _config.RegenPerSecond;
            if (regen > 0f && CurrentHp < _maxHp
                && Time.time >= _lastDamageTime + _config.RegenDelaySeconds)
            {
                _regenAccumulator += regen * Time.deltaTime;
                int whole = Mathf.FloorToInt(_regenAccumulator);
                if (whole > 0)
                {
                    _regenAccumulator -= whole;
                    Heal(whole);
                }
            }
        }
    }
}
