using UnityEngine;
using UnityEngine.Rendering;

namespace CrtRoyale
{
    /// <summary>Full-screen CRT Royale effect for the Built-in Render Pipeline. Add it to a camera.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    [AddComponentMenu("Rendering/CRT Royale")]
    public sealed class CrtRoyaleEffect : MonoBehaviour
    {
        public CrtRoyaleSettings settings = new CrtRoyaleSettings();

        CrtRoyaleRenderer m_Renderer;
        CommandBuffer m_Commands;

        void OnDisable()
        {
            m_Renderer?.Dispose();
            m_Renderer = null;
            m_Commands?.Release();
            m_Commands = null;
        }

        void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            m_Renderer ??= new CrtRoyaleRenderer();
            m_Commands ??= new CommandBuffer { name = "CRT Royale" };

            var descriptor = source.descriptor;
            descriptor.msaaSamples = 1;
            descriptor.depthBufferBits = 0;
            descriptor.useMipMap = false;
            var output = RenderTexture.GetTemporary(descriptor);

            m_Commands.Clear();
            m_Renderer.Record(m_Commands, source, output, source.width, source.height, settings, true, true, source.graphicsFormat);
            Graphics.ExecuteCommandBuffer(m_Commands);

            // Blit handles any flip and encoding needed for the back buffer.
            Graphics.Blit(output, destination);
            RenderTexture.ReleaseTemporary(output);
        }
    }
}
