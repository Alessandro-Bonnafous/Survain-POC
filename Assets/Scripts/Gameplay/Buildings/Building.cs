using UnityEngine;
using Survain.Items;

namespace Survain.Gameplay.Buildings
{
    /// <summary>
    /// Composant runtime d'une structure posée dans le monde. Au Sprint 2 issue #9 il se
    /// limite à l'ancrage de la BuildingData : c'est l'identité de la structure une fois
    /// construite (qui sert au snap des pièces voisines et aux futurs systèmes).
    ///
    /// Les HP, la dégradation visuelle et la réparation arriveront avec #10 (HP) et #11
    /// (destruction/réparation). On garde donc volontairement la surface minimale ici
    /// pour ne pas pré-câbler des choix qui dépendent de ces issues.
    ///
    /// Namespace pluriel Survain.Gameplay.Buildings (le type cardinal Building est éponyme,
    /// cf. convention Survain.Gameplay.Inventories).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Building : MonoBehaviour
    {
        [Tooltip("SO décrivant le type de structure (catégorie, taille, coût).")]
        [SerializeField] private BuildingData _data;

        public BuildingData Data => _data;

        /// <summary>
        /// Renseigne la data. Appelé par BuildModeController juste après l'instanciation.
        /// </summary>
        public void Initialize(BuildingData data)
        {
            _data = data;
            if (data != null) name = $"Building_{data.Id}";
        }
    }
}
