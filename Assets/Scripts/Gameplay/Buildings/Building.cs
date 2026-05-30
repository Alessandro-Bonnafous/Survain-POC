using System;
using UnityEngine;
using Survain.Items;

namespace Survain.Gameplay.Buildings
{
    /// <summary>
    /// Composant runtime d'une structure posée dans le monde. Porte l'identité (BuildingData)
    /// et les points de vie (#10). La dégradation visuelle et la réparation effective
    /// arriveront avec #11 — ici on pose les HP + l'API TakeDamage/Repair sur laquelle #11
    /// se branchera, sans encore gérer la destruction physique.
    ///
    /// Namespace pluriel Survain.Gameplay.Buildings (le type cardinal Building est éponyme,
    /// cf. convention Survain.Gameplay.Inventories).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Building : MonoBehaviour
    {
        [Tooltip("SO décrivant le type de structure (catégorie, taille, coût, HP).")]
        [SerializeField] private BuildingData _data;

        public BuildingData Data => _data;

        public int MaxHp { get; private set; }
        public int CurrentHp { get; private set; }

        /// <summary>Émis quand les HP changent. Signature : (current, max).</summary>
        public event Action<int, int> OnHpChanged;

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

        /// <summary>
        /// Inflige des dégâts (clampés à 0). La destruction effective à 0 HP (drop partiel,
        /// particules) est du ressort de #11 ; ici on se contente de mettre à jour l'état.
        /// </summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHp <= 0) return;
            CurrentHp = Mathf.Max(0, CurrentHp - amount);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }

        /// <summary>Répare (clampé au max).</summary>
        public void Repair(int amount)
        {
            if (amount <= 0 || CurrentHp >= MaxHp) return;
            CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);
        }
    }
}
