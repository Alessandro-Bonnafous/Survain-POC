using UnityEngine;

namespace Survain.Gameplay.Buildings
{
    /// <summary>
    /// Grille de placement affichée au sol pendant le mode construction. Self-contained
    /// et code-only (cohérent avec l'ethos du projet : pas d'asset binaire, diff Git lisible) :
    /// génère un quad horizontal + une texture de grille tuilée (1 cellule = 1 mètre).
    ///
    /// Créée et pilotée par BuildModeController. Suit la position de placement, snappée
    /// à la grille, et se masque hors mode construction.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BuildGridVisual : MonoBehaviour
    {
        private Transform _quad;
        private Material _material;
        private float _extent;
        private float _cellSize;

        /// <summary>
        /// Construit la grille. <paramref name="extent"/> = demi-côté du quad en mètres,
        /// <paramref name="cellSize"/> = taille d'une cellule (pas de la grille).
        /// </summary>
        public void Build(float extent, float cellSize)
        {
            _extent = Mathf.Max(2f, extent);
            _cellSize = Mathf.Max(0.1f, cellSize);

            var quadGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadGo.name = "GridQuad";
            // Le Quad primitive apporte un MeshCollider — on le retire (purement visuel).
            var col = quadGo.GetComponent<Collider>();
            if (col != null) Destroy(col);

            _quad = quadGo.transform;
            _quad.SetParent(transform, false);
            // Quad face -Z par défaut → on le couche dans le plan XZ, face vers le haut.
            _quad.localRotation = Quaternion.Euler(90f, 0f, 0f);
            _quad.localScale = new Vector3(_extent * 2f, _extent * 2f, 1f);

            var rend = quadGo.GetComponent<Renderer>();
            if (rend != null)
            {
                _material = BuildGridMaterial();
                int tiles = Mathf.Max(1, Mathf.RoundToInt(_extent * 2f / _cellSize));
                _material.mainTextureScale = new Vector2(tiles, tiles);
                rend.sharedMaterial = _material;
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }

            SetVisible(false);
        }

        /// <summary>Affiche/masque la grille.</summary>
        public void SetVisible(bool visible)
        {
            if (_quad != null) _quad.gameObject.SetActive(visible);
        }

        /// <summary>
        /// Centre la grille sur une position monde, légèrement au-dessus du sol pour
        /// éviter le z-fighting avec le terrain.
        /// </summary>
        public void SetCenter(Vector3 worldPosition)
        {
            transform.position = worldPosition + Vector3.up * 0.02f;
        }

        private static Material BuildGridMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);

            // Configuration transparente URP.
            mat.SetFloat("_Surface", 1f); // 0 = opaque, 1 = transparent
            mat.SetFloat("_Blend", 0f);   // alpha blend
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_SURFACE_TYPE_OPAQUE");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            var tex = BuildGridTexture();
            mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
            mat.SetColor("_BaseColor", Color.white);
            return mat;
        }

        /// <summary>
        /// Génère une cellule de grille : fond transparent, bords en blanc semi-transparent.
        /// Répétée (wrap) sur le quad pour former le quadrillage.
        /// </summary>
        private static Texture2D BuildGridTexture()
        {
            const int res = 64;
            const int lineWidth = 2;
            var tex = new Texture2D(res, res, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = "BuildGridCell",
            };

            var fill = new Color(0f, 0f, 0f, 0f);
            var line = new Color(1f, 1f, 1f, 0.35f);
            var pixels = new Color[res * res];
            for (int y = 0; y < res; y++)
            {
                for (int x = 0; x < res; x++)
                {
                    bool onLine = x < lineWidth || y < lineWidth
                        || x >= res - lineWidth || y >= res - lineWidth;
                    pixels[y * res + x] = onLine ? line : fill;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
    }
}
