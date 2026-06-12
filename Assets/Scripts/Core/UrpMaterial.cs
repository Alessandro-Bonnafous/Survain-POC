using UnityEngine;

namespace Survain.Core
{
    /// <summary>
    /// Crée des matériaux URP au runtime de façon **safe en build**. Les primitives
    /// (GameObject.CreatePrimitive) utilisent le Default-Material (shader Built-in Standard) :
    /// correct dans l'éditeur, mais **rose en build URP** (le Standard est strippé). De même, un
    /// shader URP trouvé via Shader.Find n'est inclus dans le build que s'il est listé dans
    /// Project Settings → Graphics → Always Included Shaders.
    ///
    /// Centralise donc la création d'un matériau URP coloré, à appliquer aux visuels placeholder
    /// créés par code (ennemis, tombe, portail…). Prérequis build : URP/Lit et URP/Unlit dans
    /// Always Included Shaders.
    /// </summary>
    public static class UrpMaterial
    {
        /// <summary>Remplace le matériau du renderer par un matériau URP (Lit par défaut, Unlit si
        /// demandé) de la couleur voulue. No-op si le renderer est null.</summary>
        public static void ApplyColor(Renderer renderer, Color color, bool unlit = false)
        {
            if (renderer == null) return;
            renderer.material = Create(color, unlit);
        }

        /// <summary>Instancie un matériau URP coloré (fallback Sprites/Default si URP indisponible).</summary>
        public static Material Create(Color color, bool unlit = false)
        {
            var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            mat.color = color;
            return mat;
        }
    }
}
