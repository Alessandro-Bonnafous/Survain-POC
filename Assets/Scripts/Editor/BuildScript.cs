using UnityEditor;
using UnityEditor.Build.Reporting;

namespace Survain.Editor
{
    /// <summary>
    /// Point d'entrée pour la génération du build Windows en ligne de commande (Unity batch mode).
    ///
    /// Invocation type (depuis la racine du repo) :
    ///   "&lt;chemin Unity.exe&gt;" -batchmode -nographics -quit \
    ///     -projectPath "." \
    ///     -buildTarget Win64 \
    ///     -executeMethod BuildScript.BuildWindows \
    ///     -logFile Builds/build.log
    ///
    /// La version est lue depuis PlayerSettings.bundleVersion. Le build atterrit dans
    /// Builds/win64/&lt;version&gt;/SURVAIN.exe (dossier gitignored via /[Bb]uilds/).
    ///
    /// Exception à la convention SurvainLog : on utilise UnityEngine.Debug directement
    /// car ce script tourne en batch mode où les defines UNITY_EDITOR/DEVELOPMENT_BUILD
    /// requis par SurvainLog ne sont pas garantis. Unique exception autorisée dans le
    /// projet (code Editor de build pipeline uniquement — cf. CLAUDE.md).
    /// </summary>
    public static class BuildScript
    {
        private static readonly string[] Scenes = { "Assets/Scenes/Main.unity" };

        public static void BuildWindows()
        {
            string version = PlayerSettings.bundleVersion;
            string outputPath = $"Builds/win64/{version}/SURVAIN.exe";

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded)
            {
                UnityEngine.Debug.LogError($"[BuildScript] Build échoué : {report.summary.result}");
                EditorApplication.Exit(1);
            }
            else
            {
                UnityEngine.Debug.Log($"[BuildScript] Build réussi → {outputPath}");
                EditorApplication.Exit(0);
            }
        }
    }
}
