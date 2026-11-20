using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace CrtRoyale
{
    /// <summary>Full-screen CRT Royale effect for URP. Add it to a Universal Renderer asset.</summary>
    public sealed class CrtRoyaleFeature : ScriptableRendererFeature
    {
        public CrtRoyaleSettings settings = new CrtRoyaleSettings();

        CrtRoyalePass m_Pass;

        public override void Create()
        {
            m_Pass?.Dispose();
            m_Pass = new CrtRoyalePass(settings);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Game)
                renderer.EnqueuePass(m_Pass);
        }

        protected override void Dispose(bool disposing)
        {
            m_Pass?.Dispose();
            m_Pass = null;
        }

        sealed class CrtRoyalePass : ScriptableRenderPass
        {
            sealed class PassData
            {
                public CrtRoyaleRenderer renderer;
                public CrtRoyaleSettings settings;
                public TextureHandle source;
                public UnityEngine.Experimental.Rendering.GraphicsFormat sourceFormat;
                public TextureHandle destination;
                public int width;
                public int height;
            }

            readonly CrtRoyaleSettings m_Settings;
            readonly Dictionary<Camera, CrtRoyaleRenderer> m_Renderers = new Dictionary<Camera, CrtRoyaleRenderer>();
            readonly List<Camera> m_DeadCameras = new List<Camera>();

            public CrtRoyalePass(CrtRoyaleSettings settings)
            {
                m_Settings = settings;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                requiresIntermediateTexture = true;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var resources = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();
                // In a camera stack, only the last camera's finished image gets the effect.
                if (resources.isActiveTargetBackBuffer || !cameraData.resolveFinalTarget)
                    return;

                var source = resources.activeColorTexture;
                var descriptor = renderGraph.GetTextureDesc(source);
                descriptor.name = "CrtRoyale_Destination";
                descriptor.clearBuffer = false;
                descriptor.msaaSamples = MSAASamples.None;
                descriptor.bindTextureMS = false;
                var destination = renderGraph.CreateTexture(descriptor);

                using (var builder = renderGraph.AddUnsafePass<PassData>("CRT Royale", out var data))
                {
                    data.renderer = GetRenderer(cameraData.camera);
                    data.settings = m_Settings;
                    data.source = source;
                    data.sourceFormat = descriptor.format;
                    data.destination = destination;
                    data.width = cameraData.cameraTargetDescriptor.width;
                    data.height = cameraData.cameraTargetDescriptor.height;

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.UseTexture(destination, AccessFlags.WriteAll);
                    builder.AllowPassCulling(false);
                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (PassData d, UnsafeGraphContext context) =>
                    {
                        var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                        d.renderer.Record(cmd, d.source, d.destination, d.width, d.height, d.settings, true, true, d.sourceFormat);
                    });
                }

                resources.cameraColor = destination;
            }

            CrtRoyaleRenderer GetRenderer(Camera camera)
            {
                if (m_Renderers.TryGetValue(camera, out var renderer))
                    return renderer;

                m_DeadCameras.Clear();
                foreach (var pair in m_Renderers)
                    if (pair.Key == null)
                        m_DeadCameras.Add(pair.Key);
                foreach (var dead in m_DeadCameras)
                {
                    m_Renderers[dead].Dispose();
                    m_Renderers.Remove(dead);
                }

                renderer = new CrtRoyaleRenderer();
                m_Renderers.Add(camera, renderer);
                return renderer;
            }

            public void Dispose()
            {
                foreach (var renderer in m_Renderers.Values)
                    renderer.Dispose();
                m_Renderers.Clear();
            }
        }
    }
}
