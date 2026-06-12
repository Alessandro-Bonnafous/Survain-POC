using UnityEngine;
using Unity.AI.Navigation;
using Survain.Core;

namespace Survain.AI.Npc
{
    /// <summary>
    /// Bake le NavMesh au runtime, après la génération du terrain. Indispensable car le
    /// terrain est généré au Play (cf. TerrainGenerator, mesh non versionné) → impossible de
    /// baker en éditeur. À poser sur un GameObject portant un NavMeshSurface (typiquement
    /// _WorldRoot ou un objet dédié).
    ///
    /// Ordre d'exécution +150 : après TerrainGenerator (-100) qui peuple le MeshCollider,
    /// avant NpcSpawner (+200) qui place les PNJ sur le NavMesh fraîchement baké.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshSurface))]
    [DefaultExecutionOrder(150)]
    public sealed class NavMeshRuntimeBaker : MonoBehaviour
    {
        private void Start() => Rebake();

        /// <summary>(Re)bake le NavMesh. Appelé au Start (après génération du terrain) et à chaque
        /// régénération de l'instance zone sauvage (#74) pour couvrir le nouveau terrain.</summary>
        public void Rebake()
        {
            var surface = GetComponent<NavMeshSurface>();
            surface.BuildNavMesh();
            SurvainLog.Info(SurvainLog.Category.World, "NavMesh baké au runtime.", this);
        }
    }
}
