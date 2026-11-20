using CrtRoyale;
using CrtRoyale.TestKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class CrtTestEntry
{
    const string Folder = "Assets/Settings";

    public static void Run()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        CrtCaptureSuite.ReportShaderErrors();

        var feature = ConfigurePipeline(false, 1);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        var camera = CrtTestScene.Build();
        camera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        Debug.Log($"CRT_PIPELINE active={GraphicsSettings.currentRenderPipeline?.GetType().Name}");
        CrtCaptureSuite.Run("urp", camera, feature.settings, on => feature.SetActive(on));
    }

    public static void BuildPlayer()
    {
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.runInBackground = true;
        ConfigurePipeline(true, 4);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
        EditorSceneManager.SaveScene(scene, "Assets/Empty.unity");
        var report = UnityEditor.BuildPipeline.BuildPlayer(new[] { "Assets/Empty.unity" },
            "../Build/CrtTestPlayerURP/CrtTestPlayerURP.exe", BuildTarget.StandaloneWindows64, BuildOptions.None);
        Debug.Log($"CRT_PLAYER_BUILD {report.summary.result} errors={report.summary.totalErrors}");
    }

    static CrtRoyaleFeature ConfigurePipeline(bool hdr, int msaa)
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets", "Settings");

        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        rendererData.postProcessData = null;
        AssetDatabase.CreateAsset(rendererData, Folder + "/Renderer.asset");

        var feature = ScriptableObject.CreateInstance<CrtRoyaleFeature>();
        feature.name = "CRT Royale";
        AssetDatabase.AddObjectToAsset(feature, rendererData);
        rendererData.rendererFeatures.Add(feature);
        var serialized = new SerializedObject(rendererData);
        var map = serialized.FindProperty("m_RendererFeatureMap");
        map.arraySize = 1;
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
        map.GetArrayElementAtIndex(0).longValue = localId;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rendererData);

        var asset = UniversalRenderPipelineAsset.Create(rendererData);
        AssetDatabase.CreateAsset(asset, Folder + "/Pipeline.asset");
        asset.msaaSampleCount = msaa;
        asset.supportsHDR = hdr;
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();

        GraphicsSettings.defaultRenderPipeline = asset;
        QualitySettings.renderPipeline = asset;
        return feature;
    }
}
