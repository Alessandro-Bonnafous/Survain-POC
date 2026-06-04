using System;
using UnityEngine;
using Survain.Core;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Besoins d'un PNJ (#13) : jauges Faim / Abri / Moral en [0..1] (1 = satisfait).
    ///
    /// Phase 1 — modèle + décroissance + agrégation du moral + multiplicateur de productivité,
    /// SANS comportement (l'EatingState et la désertion = phase 2) ni UI (phase 3) :
    /// - Faim décroît avec le temps (remontée en mangeant via Feed(), branché en phase 2).
    /// - Abri : stub neutre (1) au POC ; piloté par l'assignation d'un lit en #19 via SetSheltered().
    /// - Moral : converge vers une cible = moyenne pondérée (faim, abri, qualité de travail,
    ///   décalage d'événements). La qualité de travail (crochet #14) et les événements sont neutres
    ///   par défaut.
    ///
    /// Tuning sur NPCData (équilibrage Pascal). Lecture des jauges via les propriétés ; les champs
    /// runtime sont [SerializeField] uniquement pour être observables dans l'Inspector en Play.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NpcNeeds : MonoBehaviour
    {
        [Tooltip("Si le PNJ peut déserter au moral zéro. Faux pour le contremaître (#14) : il reste " +
                 "toujours présent comme point d'interaction du village.")]
        [SerializeField] private bool _canDesert = true;

        [Header("État (runtime, lecture seule)")]
        [SerializeField, Range(0f, 1f)] private float _hunger = 1f;
        [SerializeField, Range(0f, 1f)] private float _shelter = 1f;
        [SerializeField, Range(0f, 1f)] private float _morale = 1f;

        private NPCData _data;
        private float _workQuality = 1f;  // crochet #14 (1 = neutre)
        private float _eventOffset = 0f;  // crochet événements

        public float Hunger => _hunger;
        public float Shelter => _shelter;
        public float Morale => _morale;

        /// <summary>Le PNJ a faim et devrait aller manger (consommé en phase 2).</summary>
        public bool IsHungry => _data != null && _hunger <= _data.HungerSeekThreshold;

        /// <summary>Moral critique → désertion (consommé en phase 2). Faux si le PNJ ne peut pas déserter (contremaître).</summary>
        public bool IsDeserting => _canDesert && _data != null && _morale <= _data.MoraleDesertionThreshold;

        /// <summary>Moral bas → bulle d'alerte (consommé par l'UI phase 3).</summary>
        public bool IsMoraleLow => _data != null && _morale <= _data.MoraleWarnThreshold;

        /// <summary>Multiplicateur de vitesse de travail dérivé du moral (pour #14).</summary>
        public float WorkSpeedMultiplier =>
            _data == null ? 1f : Mathf.Lerp(_data.WorkSpeedAtZeroMorale, _data.WorkSpeedAtFullMorale, _morale);

        /// <summary>Notifié à chaque mise à jour des jauges (consommé par l'UI en phase 3).</summary>
        public event Action<NpcNeeds> OnNeedsChanged;

        /// <summary>Pilotée par l'assignation d'abri/lit (#19). Stub au POC.</summary>
        public void SetSheltered(bool sheltered) => _shelter = sheltered ? 1f : 0f;

        /// <summary>Pilotée par le métier (#14) : qualité du travail [0..1] qui nourrit le moral.</summary>
        public void SetWorkQuality(float quality) => _workQuality = Mathf.Clamp01(quality);

        /// <summary>Applique un décalage d'événement au moral cible (ex. fête +, deuil −).</summary>
        public void ApplyMoraleEvent(float delta) => _eventOffset = Mathf.Clamp(_eventOffset + delta, -1f, 1f);

        /// <summary>Restaure la faim (appelé par l'EatingState en phase 2).</summary>
        public void Feed(float amount01) => _hunger = Mathf.Clamp01(_hunger + amount01);

        private void Start()
        {
            var ctrl = GetComponent<NpcController>();
            if (ctrl != null) _data = ctrl.Data;
            if (_data == null)
            {
                SurvainLog.Warn(SurvainLog.Category.AI,
                    "NpcNeeds : NPCData introuvable sur le PNJ (besoins inertes).", this);
            }
        }

        private void Update()
        {
            if (_data == null) return;
            float dt = Time.deltaTime;

            // Faim décroît avec le temps.
            _hunger = Mathf.Clamp01(_hunger - _data.HungerDecayPerSecond * dt);

            // Moral cible : un socle de survie (moyenne pondérée faim + abri), modulé par la
            // qualité de travail (multiplicateur, 1 = neutre) et décalé par les événements.
            // Multiplicatif (et non additif) pour que des besoins au plus bas puissent réellement
            // effondrer le moral jusqu'à la désertion (sinon plancher artificiel).
            float wH = _data.MoraleHungerWeight;
            float wS = _data.MoraleShelterWeight;
            float wSum = Mathf.Max(0.0001f, wH + wS);
            float survival = (_hunger * wH + _shelter * wS) / wSum;
            float target = Mathf.Clamp01(survival * _workQuality + _eventOffset);
            _morale = Mathf.MoveTowards(_morale, target, _data.MoraleLerpSpeed * dt);

            OnNeedsChanged?.Invoke(this);
        }

        [ContextMenu("DEBUG/Log besoins")]
        private void DebugLog() =>
            SurvainLog.Info(SurvainLog.Category.AI,
                $"Besoins {name} : faim {_hunger:0.00} | abri {_shelter:0.00} | moral {_morale:0.00} " +
                $"| prod ×{WorkSpeedMultiplier:0.00} (hungry={IsHungry}, deserting={IsDeserting})", this);

        [ContextMenu("DEBUG/Vider la faim (test repas)")]
        private void DebugEmptyHunger() => _hunger = 0f;

        [ContextMenu("DEBUG/Effondrer le moral (test désertion)")]
        private void DebugCollapseMorale() { _morale = 0f; _eventOffset = -1f; }
    }
}
