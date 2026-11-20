using CrtRoyale;
using CrtRoyale.TestKit;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CrtTestEntry
{
    public static void Run()
    {
        PlayerSettings.colorSpace = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-crtGamma") >= 0
            ? ColorSpace.Gamma : ColorSpace.Linear;
        CrtCaptureSuite.ReportShaderErrors();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var camera = CrtTestScene.Build();
        var effect = camera.gameObject.AddComponent<CrtRoyaleEffect>();
        string name = PlayerSettings.colorSpace == ColorSpace.Gamma ? "builtin-gamma" : "builtin";
        CrtCaptureSuite.Run(name, camera, effect.settings, on => effect.enabled = on);
    }

    public static void BuildPlayer()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.runInBackground = true;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        EditorSceneManager.SaveScene(scene, "Assets/Empty.unity");
        var report = BuildPipeline.BuildPlayer(new[] { "Assets/Empty.unity" },
            "../Build/CrtTestPlayer/CrtTestPlayer.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log($"CRT_PLAYER_BUILD {report.summary.result} errors={report.summary.totalErrors}");
    }
}
