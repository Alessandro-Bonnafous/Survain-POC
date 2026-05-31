using System;
using System.Collections.Generic;
using UnityEngine;
using Survain.Core;
using Survain.Gameplay.Inventories;
using Survain.Items;

namespace Survain.Gameplay.Buildings
{
    /// <summary>
    /// Composant runtime d'une structure posée dans le monde. Porte l'identité (BuildingData)
    /// et les points de vie. La destruction à 0 HP (remboursement partiel + déversement d'un
    /// coffre + feedback) et la réparation sont gérées ici (#11). Les dégâts proviennent du
    /// PlayerBuildingTool (clic gauche) au POC ; du combat au Sprint 4.
    ///
    /// Namespace pluriel Survain.Gameplay.Buildings (le type cardinal Building est éponyme,
    /// cf. convention Survain.Gameplay.Inventories).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Building : MonoBehaviour
    {
        [Tooltip("SO décrivant le type de structure (catégorie, taille, coût, HP, remboursement).")]
        [SerializeField] private BuildingData _data;

        [Tooltip("Couleur émissive quand le joueur vise le bâtiment (outil de démolition).")]
        [SerializeField] private Color _highlightEmission = new Color(1f, 0.6f, 0.4f) * 0.5f;

        [Tooltip("Teinte vers laquelle le bâtiment vire quand il est très endommagé (0 HP).")]
        [SerializeField] private Color _damagedColor = new Color(0.25f, 0.2f, 0.18f);

        public BuildingData Data => _data;

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }

        // Registre statique des bâtiments présents en scène (alternative à FindObjectsOfType,
        // proscrit en runtime). Alimente les requêtes spatiales des PNJ (ex. feu de camp le plus
        // proche pour manger, #13 ; chantier/coffre le plus proche, #14).
        private static readonly List<Building> _all = new List<Building>();
        public static IReadOnlyList<Building> All => _all;

        /// <summary>Bâtiment le plus proche de <paramref name="from"/> satisfaisant le filtre (ou null).</summary>
        public static Building FindNearest(Vector3 from, Predicate<Building> filter = null)
        {
            Building best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < _all.Count; i++)
            {
                var b = _all[i];
                if (b == null || (filter != null && !filter(b))) continue;
                float d = (b.transform.position - from).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = b; }
            }
            return best;
        }

        private void OnEnable() => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

        /// <summary>Émis quand les HP changent. Signature : (current, max).</summary>
        public event Action<int, int> OnHpChanged;

        private Renderer[] _renderers;
        private Color[] _baseColors;
        private bool _materialsCached;
        private bool _highlighted;
        private bool _destroyed;

        /// <summary>
        /// Renseigne la data et initialise les HP au max. Appelé juste après l'instanciation
        /// (par ConstructionSite à la complétion du chantier).
        /// </summary>
        public void Initialize(BuildingData data)
        {
            _data = data;
            if (data != null)
            {
                name = $"Building_{data.Id}";
                MaxHp = Mathf.Max(1, data.MaxHp);
                CurrentHp = MaxHp;
            }
        }

        /// <summary>Inflige des dégâts (clampés à 0). À 0 HP, le bâtiment est détruit.</summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHp <= 0) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            if (CurrentHp == 0) DestroyBuilding();
            else ApplyDamageTint();
        }

        /// <summary>Répare (clampé au max).</summary>
        public void Repair(int amount)
        {
            if (amount <= 0 || CurrentHp >= MaxHp) return;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
            ApplyDamageTint();
        }

        /// <summary>Active/désactive la surbrillance émissive (visée par l'outil de démolition).</summary>
        public void SetHighlighted(bool highlighted)
        {
            if (_highlighted == highlighted) return;
            _highlighted = highlighted;
            EnsureMaterialCache();

            Color emission = highlighted ? _highlightEmission : Color.black;
            for (int i = 0; i < _renderers.Length; i++)
            {
                var rend = _renderers[i];
                if (rend == null) continue;
                var mat = rend.material; // même clone que la teinte de dégâts (un par renderer)
                if (mat == null || !mat.HasProperty("_EmissionColor")) continue;
                if (highlighted) mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }
        }

        /// <summary>
        /// Cache les Renderers et leur couleur de base (au premier accès). Le clone de
        /// material via Renderer.material est partagé entre la surbrillance (émissive) et la
        /// teinte de dégâts (couleur de base) — un seul clone par renderer.
        /// </summary>
        private void EnsureMaterialCache()
        {
            if (_materialsCached) return;
            _materialsCached = true;
            _renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                var rend = _renderers[i];
                if (rend == null) continue;
                var mat = rend.material;
                _baseColors[i] = mat != null && mat.HasProperty("_BaseColor")
                    ? mat.GetColor("_BaseColor")
                    : (mat != null ? mat.color : Color.white);
            }
        }

        /// <summary>Teinte le bâtiment de sa couleur de base (plein HP) vers _damagedColor (0 HP).</summary>
        private void ApplyDamageTint()
        {
            EnsureMaterialCache();
            float frac = MaxHp > 0 ? (float)CurrentHp / MaxHp : 1f;
            for (int i = 0; i < _renderers.Length; i++)
            {
                var rend = _renderers[i];
                if (rend == null) continue;
                var mat = rend.material;
                if (mat == null) continue;
                Color c = Color.Lerp(_damagedColor, _baseColors[i], frac);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
                mat.color = c;
            }
        }

        private void DestroyBuilding()
        {
            if (_destroyed) return;
            _destroyed = true;

            Vector3 pos = transform.position + Vector3.up * 0.5f;

            // Remboursement partiel du coût (ratio paramétrable, en attente d'arbitrage Pascal).
            if (_data != null && _data.Cost != null)
            {
                foreach (var c in _data.Cost)
                {
                    if (c.Item == null) continue;
                    int give = Mathf.FloorToInt(c.Amount * _data.RefundRatio);
                    if (give > 0) WorldItemSpawner.Spawn(c.Item, give, pos);
                }
            }

            // Un coffre déverse son contenu pour ne pas le perdre silencieusement.
            var storage = GetComponent<StorageContainer>();
            if (storage != null) storage.SpillContents();

            // Feedback de destruction (burst détaché qui survit au Destroy).
            var color = _data != null ? BuildingVisualFactory.ColorFor(_data.Category) : Color.gray;
            BuildingDestructionFx.Spawn(pos, color);

            SurvainLog.Info(SurvainLog.Category.Gameplay,
                $"Bâtiment '{(_data != null ? _data.Id : "?")}' détruit.", this);

            Destroy(gameObject);
        }
    }
}
