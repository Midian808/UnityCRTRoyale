using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CrtRoyale.TestKit
{
    /// <summary>Renders the test scene through one pipeline's effect and writes PNGs for inspection.</summary>
    public static class CrtCaptureSuite
    {
        public static void Run(string pipeline, Camera camera, CrtRoyaleSettings settings, Action<bool> setEffectEnabled)
        {
            var dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", ".tmp", "captures", pipeline));
            Directory.CreateDirectory(dir);

            setEffectEnabled(false);
            Capture(camera, 1920, 1080, 1, Path.Combine(dir, "00_input.png"));

            setEffectEnabled(true);
            Capture(camera, 1920, 1080, 70, Path.Combine(dir, "01_default.png"));

            // A settings change must show on the very next frame, not after the refresh cycle.
            var type = settings.maskType;
            settings.maskType = MaskType.Grille;
            settings.maskTriadWidth = 6f;
            Capture(camera, 1920, 1080, 1, Path.Combine(dir, "02_grille_first_frame.png"));
            Capture(camera, 1920, 1080, 70, Path.Combine(dir, "03_grille_settled.png"));
            settings.maskType = type;
            settings.maskTriadWidth = 3f;

            // A resize must reallocate cleanly, then return to the original output.
            Capture(camera, 1280, 720, 1, Path.Combine(dir, "04_resized_1280.png"));
            Capture(camera, 1920, 1080, 70, Path.Combine(dir, "05_back_to_default.png"));

            settings.geometryMode = GeometryMode.Spherical;
            settings.antialiasLevel = 8;
            Capture(camera, 1920, 1080, 2, Path.Combine(dir, "06_curved_aa8.png"));
            settings.geometryMode = GeometryMode.Flat;
            settings.antialiasLevel = 0;

            settings.beamShape = BeamShape.MultiSourceGaussian;
            settings.maskType = MaskType.Shadow;
            settings.scanlineThickness = 4;
            Capture(camera, 1920, 1080, 2, Path.Combine(dir, "07_gaussian_shadow.png"));
            settings.beamShape = BeamShape.Linear;
            settings.maskType = type;
            settings.scanlineThickness = 2;

            settings.enableInterlacing = true;
            settings.deinterlacingMode = DeinterlacingMode.BlendedWeaving;
            Capture(camera, 1920, 1080, 4, Path.Combine(dir, "08_interlaced_weave.png"));
            settings.enableInterlacing = false;
            settings.deinterlacingMode = DeinterlacingMode.FakeProgressive;

            settings.enablePreblur = false;
            Capture(camera, 1920, 1080, 2, Path.Combine(dir, "09_preblur_off.png"));
            settings.enablePreblur = true;

            settings.useFullFrame = false;
            settings.contentSize = new Vector2Int(1440, 1080);
            Capture(camera, 1920, 1080, 2, Path.Combine(dir, "10_cropped_4x3.png"));
            settings.showContentBox = true;
            Capture(camera, 1920, 1080, 1, Path.Combine(dir, "11_content_box.png"));
            settings.showContentBox = false;
            settings.useFullFrame = true;

            Debug.Log($"CRT_CAPTURE_DONE {pipeline} {dir} colorSpace={QualitySettings.activeColorSpace} api={SystemInfo.graphicsDeviceType}");
        }

        static void Capture(Camera camera, int width, int height, int frames, string path)
        {
            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            target.Create();
            camera.targetTexture = target;
            for (int i = 0; i < frames; i++)
                camera.Render();

            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = null;
            camera.targetTexture = null;
            File.WriteAllBytes(path, image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
        }

        public static void ReportShaderErrors()
        {
            var shader = Resources.Load<Shader>("CrtRoyale");
            bool errors = shader == null || ShaderUtil.ShaderHasError(shader);
            Debug.Log($"CRT_SHADER_STATUS loaded={shader != null} hasError={errors} passes={(shader == null ? 0 : shader.passCount)}");
            if (shader != null)
                foreach (var message in ShaderUtil.GetShaderMessages(shader))
                    Debug.Log($"CRT_SHADER_MESSAGE {message.severity} line {message.line} {message.file}: {message.message}");
        }
    }
}
