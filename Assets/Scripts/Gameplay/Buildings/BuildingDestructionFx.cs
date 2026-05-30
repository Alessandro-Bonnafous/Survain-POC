using UnityEngine;

namespace Survain.Gameplay.Buildings
{
    /// <summary>
    /// Burst de particules joué à la destruction d'un bâtiment. Spawné en GameObject
    /// standalone (détaché) pour survivre au Destroy du bâtiment — même pattern que le
    /// burst de fin de vie d'un ResourceNode (cf. ResourceNodeJuice).
    ///
    /// Code-only (pas d'asset) : material URP/Unlit + mesh cube partagés statiquement.
    /// </summary>
    internal static class BuildingDestructionFx
    {
        private static Material _sharedMaterial;
        private static Mesh _sharedMesh;

        public static void Spawn(Vector3 position, Color color, int particleCount = 24)
        {
            var go = new GameObject("BuildingDestructionFx");
            go.transform.position = position;

            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1.2f;
            main.loop = false;
            main.startLifetime = 0.9f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startColor = color;
            main.gravityModifier = 1.2f;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f; // burst manuel uniquement

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = GetSharedMaterial();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            renderer.mesh = GetSharedMesh();

            ps.Emit(particleCount);
            ps.Play();

            // Sécurité si stopAction ne déclenche pas (ex. domaine rechargé).
            Object.Destroy(go, 5f);
        }

        private static Material GetSharedMaterial()
        {
            if (_sharedMaterial != null) return _sharedMaterial;
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            _sharedMaterial = new Material(shader);
            return _sharedMaterial;
        }

        private static Mesh GetSharedMesh()
        {
            if (_sharedMesh != null) return _sharedMesh;
            var temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _sharedMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(temp);
            return _sharedMesh;
        }
    }
}
