using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Kenta.EditorTools
{
    /// <summary>
    /// Build Web pour la borne (ADR-031) : gabarit « Kenta » (plein écran sans défilement, rendu fixe en 1920×1080),
    /// build de release (jamais « Development Build » : overlay, IMGUI et vérifications en plus coûtent cher sur la borne),
    /// IL2CPP optimisé pour la vitesse. Menu Kenta > Build Web (borne), puis tools/package-build.sh.
    /// En ligne de commande : -executeMethod Kenta.EditorTools.KentaWebBuild.Build (ou .ApplySettings, sans module Web).
    /// </summary>
    public static class KentaWebBuild
    {
        const string Template = "PROJECT:Kenta";
        const string OutDir = "Build";                          // game/Build (gitignoré)

        [MenuItem("Kenta/Régler le projet pour la borne (Web)")]
        public static void ApplySettings()
        {
            PlayerSettings.WebGL.template = Template;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.SetIl2CppCompilerConfiguration(NamedBuildTarget.WebGL, Il2CppCompilerConfiguration.Release);
            PlayerSettings.SetIl2CppCodeGeneration(NamedBuildTarget.WebGL, Il2CppCodeGeneration.OptimizeSpeed);
            PlayerSettings.defaultWebScreenWidth = 1920;
            PlayerSettings.defaultWebScreenHeight = 1080;
            PlayerSettings.runInBackground = true;
            EditorUserBuildSettings.development = false;
            AssetDatabase.SaveAssets();
            Debug.Log("[KentaWebBuild] réglages de la borne appliqués (gabarit Kenta, release, IL2CPP vitesse)");
        }

        [MenuItem("Kenta/Build Web (borne)")]
        public static void Build()
        {
            ApplySettings();
            var scenes = new[] { "Assets/_Project/Scenes/Main.unity" };
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None,                   // surtout pas Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            Debug.Log($"[KentaWebBuild] {s.result} : {Path.GetFullPath(OutDir)}, {s.totalSize / (1024 * 1024)} Mo, {s.totalTime.TotalMinutes:F1} min");
            if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}
