using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Command-line entry points used by the Unity CLI (`unity build ... --execute-method`).</summary>
public static class BuildScript
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    [MenuItem("Spice Route/Setup Scene")]
    public static void SetupScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var go = new GameObject("Game");
        go.AddComponent<Game>();
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.001f;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.skybox = null;
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log("[SpiceRoute] Scene ready: " + ScenePath);
    }

    public static void ConfigurePlayer()
    {
        PlayerSettings.productName = "The Spice Route: The Malabar Expedition";
        PlayerSettings.companyName = "Spice Route Team";
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultWebScreenWidth = 1280;
        PlayerSettings.defaultWebScreenHeight = 720;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
        PlayerSettings.WebGL.template = "PROJECT:SpiceRoute";
        PlayerSettings.WebGL.showDiagnostics = false;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.Minimal);
        PlayerSettings.stripEngineCode = false;
        for (int i = 0; i < QualitySettings.names.Length; i++) { }
        AssetDatabase.SaveAssets();
    }

    static string Arg(string name, string fallback)
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
            if (args[i] == name) return args[i + 1];
        return fallback;
    }

    public static void BuildWebGL()
    {
        SetupScene();
        ConfigurePlayer();
        string output = Arg("-buildOutput", "Builds/WebGL");
        Debug.Log("[SpiceRoute] Building WebGL to " + output);
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = output,
            target = BuildTarget.WebGL,
            targetGroup = BuildTargetGroup.WebGL,
            options = BuildOptions.None,
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[SpiceRoute] Build result: " + report.summary.result + "  size: " + report.summary.totalSize + "  errors: " + report.summary.totalErrors);
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    public static void BuildWindows()
    {
        SetupScene();
        ConfigurePlayer();
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;
        string output = Arg("-buildOutput", "Builds/Win/SpiceRoute.exe");
        var opts = new BuildPlayerOptions { scenes = new[] { ScenePath }, locationPathName = output, target = BuildTarget.StandaloneWindows64, options = BuildOptions.None };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[SpiceRoute] Windows build result: " + report.summary.result);
        if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    /// <summary>Compile check + scene setup without building.</summary>
    public static void Prepare()
    {
        SetupScene();
        ConfigurePlayer();
        Debug.Log("[SpiceRoute] Prepare done");
    }
}
