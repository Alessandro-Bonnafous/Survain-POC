using UnityEngine;

namespace Survain.Items
{
    /// <summary>
    /// Bâtiment / structure constructible. Enrichi au Sprint 2 (issue #9) pour porter
    /// les données nécessaires au système de placement : catégorie, encombrement,
    /// coût en ressources, et prefab visuel optionnel.
    ///
    /// Conteneur de données pur (aucune logique gameplay) — le runtime est porté par
    /// le composant Survain.Gameplay.Buildings.Building et piloté par BuildModeController.
    ///
    /// Convention POC : si <see cref="Prefab"/> est null, le système génère un placeholder
    /// cube dimensionné par <see cref="Size"/> et coloré selon <see cref="Category"/>.
    /// Les vrais prefabs modulaires arriveront avec l'issue #10.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BuildingData",
        menuName = "Survain/Items/Building",
        order = 54)]
    public sealed class BuildingData : ItemData
    {
        [Header("Construction")]
        [Tooltip("Famille de la structure (fondation, mur, toit, fonctionnelle...).")]
        [SerializeField] private BuildCategory _category = BuildCategory.Foundation;

        [Tooltip("Encombrement de la structure en mètres (boîte englobante). Sert au placeholder visuel ET au test de collision au placement.")]
        [SerializeField] private Vector3 _size = new Vector3(1f, 1f, 1f);

        [Tooltip("Coût en ressources déduit de l'inventaire à la pose. Vide = gratuit (debug).")]
        [SerializeField] private BuildCost[] _cost = new BuildCost[0];

        [Tooltip("Prefab du visuel à instancier. Optionnel : si null, un placeholder coloré dimensionné par Size est généré (POC, avant les prefabs modulaires de #10).")]
        [SerializeField] private GameObject _prefab;

        [Header("Solidité & fonction")]
        [Tooltip("Points de vie de la structure une fois construite (socle de la destruction/réparation #11).")]
        [Min(1)]
        [SerializeField] private int _maxHp = 100;

        [Tooltip("Capacité de stockage en slots. 0 = pas un conteneur. >0 = la structure devient un coffre (Inventory secondaire).")]
        [Min(0)]
        [SerializeField] private int _storageCapacity = 0;

        [Header("Lumière (feu de camp, torche…)")]
        [Tooltip("Si la structure émet de la lumière une fois construite (source ponctuelle).")]
        [SerializeField] private bool _emitsLight = false;

        [Tooltip("Couleur de la lumière émise.")]
        [SerializeField] private Color _lightColor = new Color(1f, 0.6f, 0.25f);

        [Tooltip("Portée de la lumière (mètres).")]
        [Min(0f)]
        [SerializeField] private float _lightRange = 8f;

        [Tooltip("Intensité de la lumière.")]
        [Min(0f)]
        [SerializeField] private float _lightIntensity = 3f;

        [Tooltip("Hauteur de la source lumineuse au-dessus du pivot au sol (mètres).")]
        [SerializeField] private float _lightHeight = 1f;

        public BuildCategory Category => _category;
        public Vector3 Size => _size;
        public BuildCost[] Cost => _cost;
        public GameObject Prefab => _prefab;
        public int MaxHp => _maxHp;
        public int StorageCapacity => _storageCapacity;
        public bool EmitsLight => _emitsLight;
        public Color LightColor => _lightColor;
        public float LightRange => _lightRange;
        public float LightIntensity => _lightIntensity;
        public float LightHeight => _lightHeight;

        public override ItemType Type => ItemType.Building;
    }
}
