using UnityEngine;

namespace CrtRoyale.TestKit
{
    /// <summary>Builds a deterministic, unlit scene so every pipeline and capture sees the same input.</summary>
    public static class CrtTestScene
    {
        public const int ArtWidth = 480;
        public const int ArtHeight = 270;

        public static Camera Build()
        {
            var cameraObject = new GameObject("Test Camera");
            cameraObject.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = false;
            camera.fieldOfView = 40f;
            camera.allowMSAA = false;
            camera.allowHDR = false;

            var unlit = Resources.Load<Shader>("CrtTestUnlit");

            var art = GameObject.CreatePrimitive(PrimitiveType.Quad);
            art.name = "Upscaled Pixel Art";
            float height = 2f * 20f * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            art.transform.SetPositionAndRotation(new Vector3(0f, 0f, 10f), Quaternion.identity);
            art.transform.localScale = new Vector3(height * 16f / 9f, height, 1f);
            art.GetComponent<Renderer>().sharedMaterial = new Material(unlit) { mainTexture = MakePixelArt() };

            var checker = new Material(unlit) { mainTexture = MakeChecker() };
            AddSolid(PrimitiveType.Cube, new Vector3(2.4f, 0.7f, 0f), new Vector3(25f, 35f, 10f), 1.7f, checker);
            AddSolid(PrimitiveType.Sphere, new Vector3(4.2f, -1.1f, -1f), Vector3.zero, 1.6f, checker);
            return camera;
        }

        static void AddSolid(PrimitiveType type, Vector3 position, Vector3 euler, float scale, Material material)
        {
            var solid = GameObject.CreatePrimitive(type);
            solid.transform.SetPositionAndRotation(position, Quaternion.Euler(euler));
            solid.transform.localScale = Vector3.one * scale;
            solid.GetComponent<Renderer>().sharedMaterial = material;
        }

        static Texture2D MakeChecker()
        {
            var texture = new Texture2D(8, 8, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < 8; y++)
                for (int x = 0; x < 8; x++)
                    texture.SetPixel(x, y, ((x + y) & 1) == 0 ? new Color(0.95f, 0.75f, 0.2f) : new Color(0.15f, 0.3f, 0.75f));
            texture.Apply();
            return texture;
        }

        static Texture2D MakePixelArt()
        {
            var texture = new Texture2D(ArtWidth, ArtHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            var bars = new[] { Color.white, Color.yellow, Color.cyan, Color.green, Color.magenta, Color.red, Color.blue, Color.black };
            for (int y = 0; y < ArtHeight; y++)
            {
                int fromTop = ArtHeight - 1 - y;
                for (int x = 0; x < ArtWidth; x++)
                {
                    Color c;
                    if (fromTop < 90 && x < 240)
                        c = bars[x * bars.Length / 240];
                    else if (fromTop < 90)
                        c = new Color(0.08f, 0.1f, 0.25f);
                    else if (fromTop < 130)
                    {
                        float v = Mathf.Floor(x / 15f) / 31f;
                        c = new Color(v, v, v);
                    }
                    else
                        c = Sprite(x, fromTop);
                    texture.SetPixel(x, y, c);
                }
            }

            // Orientation markers: red top-left, green top-right, blue bottom-left, white bottom-right.
            Fill(texture, 0, ArtHeight - 12, 12, 12, Color.red);
            Fill(texture, ArtWidth - 12, ArtHeight - 12, 12, 12, Color.green);
            Fill(texture, 0, 0, 12, 12, Color.blue);
            Fill(texture, ArtWidth - 12, 0, 12, 12, Color.white);
            texture.Apply();
            return texture;
        }

        static Color Sprite(int x, int fromTop)
        {
            string[] invader =
            {
                "..#.....#..", "...#...#...", "..#######..", ".##.###.##.",
                "###########", "#.#######.#", "#.#.....#.#", "...##.##..."
            };
            const int scale = 4;
            int cellX = (x % 96) / scale - 6;
            int cellY = ((fromTop - 130) % 48) / scale - 2;
            bool lit = cellX >= 0 && cellX < 11 && cellY >= 0 && cellY < 8 && invader[cellY][cellX] == '#';
            var ground = new Color(0.03f, 0.05f, 0.12f);
            var color = Color.HSVToRGB((x / 96 % 5) / 5f, 0.7f, 1f);
            return lit ? color : ground;
        }

        static void Fill(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int j = y; j < y + height; j++)
                for (int i = x; i < x + width; i++)
                    texture.SetPixel(i, j, color);
        }
    }

    /// <summary>
    /// Shows the test scene in a built player. With no arguments the Built-in player shows it with no effect,
    /// as the input for a ReShade reference capture. "-crtCapture path" saves the window after it has settled,
    /// logs frame timing and quits.
    /// </summary>
    public sealed class CrtTestPlayer : MonoBehaviour
    {
        const int SettleFrames = 130;
        const int TimedFrames = 300;

        /// <summary>Set by a project to attach its effect to the camera.</summary>
        public static System.Action<Camera> ConfigureCamera;

        string m_CapturePath;
        int m_Frame;
        float m_TimedSeconds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if (Application.isEditor)
                return;
            Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            var camera = CrtTestScene.Build();
            ConfigureCamera?.Invoke(camera);

            var args = System.Environment.GetCommandLineArgs();
            int index = System.Array.IndexOf(args, "-crtCapture");
            if (index >= 0 && index + 1 < args.Length)
                camera.gameObject.AddComponent<CrtTestPlayer>().m_CapturePath = args[index + 1];
        }

        void Update()
        {
            m_Frame++;
            if (m_Frame > SettleFrames)
                m_TimedSeconds += Time.unscaledDeltaTime;
            if (m_Frame == SettleFrames + TimedFrames)
            {
                Debug.Log($"CRT_PLAYER_CAPTURE size={Screen.width}x{Screen.height} avgFrameMs={1000f * m_TimedSeconds / TimedFrames:F2} " +
                    $"gpu={SystemInfo.graphicsDeviceName} api={SystemInfo.graphicsDeviceType} hdr={GetComponent<Camera>().allowHDR} msaa={GetComponent<Camera>().allowMSAA} " +
                    $"pipeline={(UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null ? "builtin" : UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.GetType().Name)}");
                ScreenCapture.CaptureScreenshot(m_CapturePath);
            }
            if (m_Frame == SettleFrames + TimedFrames + 10)
                Application.Quit();
        }
    }
}
