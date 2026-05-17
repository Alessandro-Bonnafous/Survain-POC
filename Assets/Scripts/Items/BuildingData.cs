using UnityEngine;

namespace Survain.Items
{
    /// <summary>
    /// Bâtiment / structure constructible. Squelette POC — le système de construction
    /// arrivera vraisemblablement au Sprint 2. Pour l'instant on tient juste la donnée.
    /// </summary>
    [CreateAssetMenu(
        fileName = "BuildingData",
        menuName = "Survain/Items/Building",
        order = 54)]
    public sealed class BuildingData : ItemData
    {
        [Header("Construction")]
        [Tooltip("Prefab du bâtiment à instancier. Optionnel au stade squelette.")]
        [SerializeField] private GameObject _prefab;

        public GameObject Prefab => _prefab;

        public override ItemType Type => ItemType.Building;
    }
}
