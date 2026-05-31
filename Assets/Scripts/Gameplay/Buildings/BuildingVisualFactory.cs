using UnityEngine;
using Survain.Items;

namespace Survain.Gameplay.Buildings
{
    /// <summary>
    /// Fabrique le visuel d'une structure à partir de sa BuildingData. Factorise la
    /// création utilisée à la fois par le ghost de prévisualisation (BuildModeController)
    /// et par la structure réellement posée — même apparence dans les deux cas.
    ///
    /// Si la data porte un Prefab, on l'instancie. Sinon (cas POC avant les prefabs
    /// modulaires de #10), on génère un cube placeholder dimensionné par data.Size et
    /// coloré selon data.Category, sur shader URP Lit (même pattern que WorldItem /
    /// ResourceNode pour éviter le rose URP du shader Standard).
    ///
    /// Convention de pivot : le pivot de la structure est au sol (bas-centre). Le visuel
    /// est donc remonté de Size.y/2 pour qu'un cube centré repose sur le sol.
    /// </summary>
    internal static class BuildingVisualFactory
    {
        /// <summary>
        /// Crée le visuel sous <paramref name="parent"/> (position/rotation locales nulles)
        /// et retourne l'instance racine du visuel.
        /// </summary>
        public static GameObject Create(BuildingData data, Transform parent)
        {
            GameObject visual = data.Prefab != null
                ? CreateFromPrefab(data, parent)
                : CreatePlaceholderCube(data, parent);

            // Garantit un collider sur la racine pour le raycast (dépôt chantier, démolition,
            // interaction) et l'occupation. Les FBX importés (ex. Kenney) n'ont pas de collider
            // par défaut ; le cube placeholder, lui, en apporte un (on ne double pas).
            EnsureFootprintCollider(parent, data.Size);

            return visual;
        }

        private static GameObject CreateFromPrefab(BuildingData data, Transform parent)
        {
            var go = Object.Instantiate(data.Prefab, parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.name = "Visual";
            return go;
        }

        private static GameObject CreatePlaceholderCube(BuildingData data, Transform parent)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Visual";
            cube.transform.SetParent(parent, false);

            var size = data.Size;
            cube.transform.localScale = size;
            // Cube centré → on le remonte pour qu'il repose sur le pivot au sol.
            cube.transform.localPosition = Vector3.up * (size.y * 0.5f);

            var rend = cube.GetComponent<Renderer>();
            if (rend != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                var mat = new Material(shader);
                var color = ColorFor(data.Category);
                mat.SetColor("_BaseColor", color);
                mat.color = color;
                rend.sharedMaterial = mat;
            }

            return cube;
        }

        /// <summary>
        /// Ajoute un BoxCollider dimensionné par Size sur la racine si aucun collider n'est
        /// déjà présent dans la hiérarchie (cas d'un prefab sans collider). Centré au-dessus
        /// du pivot au sol.
        /// </summary>
        private static void EnsureFootprintCollider(Transform parent, Vector3 size)
        {
            if (parent.GetComponentInChildren<Collider>() != null) return;
            var box = parent.gameObject.AddComponent<BoxCollider>();
            box.center = Vector3.up * (size.y * 0.5f);
            box.size = size;
        }

        /// <summary>Couleur indicative du placeholder selon la famille de structure.</summary>
        public static Color ColorFor(BuildCategory category)
        {
            switch (category)
            {
                case BuildCategory.Shelter:    return new Color(0.55f, 0.45f, 0.32f); // bois/torchis
                case BuildCategory.Storage:    return new Color(0.5f, 0.4f, 0.28f);
                case BuildCategory.Functional: return new Color(0.8f, 0.55f, 0.25f);  // orange
                case BuildCategory.Foundation: return new Color(0.45f, 0.42f, 0.38f); // pierre/terre
                case BuildCategory.Wall:       return new Color(0.62f, 0.48f, 0.32f); // bois
                case BuildCategory.Roof:       return new Color(0.35f, 0.27f, 0.2f);  // bois sombre
                case BuildCategory.Door:       return new Color(0.5f, 0.35f, 0.2f);
                case BuildCategory.Window:     return new Color(0.55f, 0.7f, 0.8f);   // verre
                default:                       return Color.gray;
            }
        }
    }
}
