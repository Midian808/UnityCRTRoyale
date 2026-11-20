using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace CrtRoyale
{
    /// <summary>
    /// Records the crt-royale pass chain into a command buffer. Both render pipelines use it,
    /// so they share one implementation. Create one instance per camera, because the phosphor
    /// mask, beam table and previous-frame textures persist between that camera's frames.
    /// </summary>
    public sealed class CrtRoyaleRenderer : IDisposable
    {
        enum Pass
        {
            CaptureInput, ContentBox, PreblurVert, PreblurHoriz, BeamDist, ElectronBeam, BeamConvergence,
            BloomApproxVert, BloomApproxHoriz, BlurVertical, BlurHorizontal, Deinterlace, FreezeFrame,
            GeneratePhosphorMask, ApplyPhosphorMask, Brightpass, BloomVertical, BloomHorizontal, Geometry,
            Uncrop, PresentOutput
        }

        enum Target
        {
            BackBuffer, Output, PreblurVert, PreblurHoriz, BeamDist, ElectronBeams, BeamConvergence,
            BloomApproxVert, BloomApproxHoriz, BlurVertical, BlurHorizontal, Deinterlace, FreezeFrame,
            PhosphorMask, MaskedScanlines, Brightpass, BloomVertical, BloomHorizontal, Geometry, Count
        }

        const int FullscreenVertices = 3;
        const int StripVertices = 6;

        static readonly string[] s_GlobalNames =
        {
            "samplerBackBuffer", "_CrtOutput", "samplerPreblurVert", "samplerPreblurHoriz", "samplerBeamDist",
            "samplerElectronBeams", "samplerBeamConvergence", "samplerBloomApproxVert", "samplerBloomApproxHoriz",
            "samplerBlurVertical", "samplerBlurHorizontal", "samplerDeinterlace", "samplerFreezeFrame",
            "samplerPhosphorMask", "samplerMaskedScanlines", "samplerBrightpass", "samplerBloomVertical",
            "samplerBloomHorizontal", "samplerGeometry"
        };

        static readonly int s_Source = Shader.PropertyToID("_CrtSource");

        readonly Material m_Material;
        readonly RenderTexture[] m_Targets = new RenderTexture[(int)Target.Count];
        readonly LocalKeyword m_PreblurOff;
        Vector2Int m_BufferSize, m_ContentSize;
        int m_BeamColorSamples, m_BeamDistanceSamples;
        float m_BloomFactor;
        Hash128 m_LastSettings;
        uint m_FrameCount;

        public CrtRoyaleRenderer()
        {
            var shader = Resources.Load<Shader>("CrtRoyale");
            if (shader == null)
                throw new InvalidOperationException("CRT Royale: the CrtRoyale shader resource is missing.");
            m_Material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            m_PreblurOff = new LocalKeyword(shader, "CRT_PREBLUR_OFF");
        }

        /// <summary>
        /// Records the effect from <paramref name="source"/> to <paramref name="destination"/>, which must be
        /// render textures of the given size. The flip flags say whether each one stores the image bottom row first.
        /// </summary>
        public void Record(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination,
            int width, int height, CrtRoyaleSettings settings, bool sourceFlipY, bool destinationFlipY, GraphicsFormat sourceFormat)
        {
            bool refresh = EnsureTargets(cmd, width, height, settings);
            var hash = settings.ComputeHash();
            refresh |= hash != m_LastSettings;
            m_LastSettings = hash;

            SetGlobals(cmd, settings, refresh, sourceFlipY, destinationFlipY);
            cmd.SetGlobalFloat("_CrtSourceIs8Bit", Is8Bit(sourceFormat) ? 1f : 0f);
            cmd.SetGlobalTexture(s_Source, source);
            for (int i = 0; i < m_Targets.Length; i++)
                cmd.SetGlobalTexture(s_GlobalNames[i], m_Targets[i]);
            m_Material.SetKeyword(m_PreblurOff, !settings.enablePreblur);

            Draw(cmd, Pass.CaptureInput, Target.BackBuffer);
            if (settings.showContentBox)
            {
                Draw(cmd, Pass.ContentBox, Target.Output);
            }
            else
            {
                if (settings.enablePreblur)
                {
                    Draw(cmd, Pass.PreblurVert, Target.PreblurVert, StripVertices);
                    Draw(cmd, Pass.PreblurHoriz, Target.PreblurHoriz);
                }
                Draw(cmd, Pass.BeamDist, Target.BeamDist, FullscreenVertices, true);
                Draw(cmd, Pass.ElectronBeam, Target.ElectronBeams, settings.enablePreblur ? FullscreenVertices : StripVertices);
                Draw(cmd, Pass.BeamConvergence, Target.BeamConvergence);
                Draw(cmd, Pass.BloomApproxVert, Target.BloomApproxVert);
                Draw(cmd, Pass.BloomApproxHoriz, Target.BloomApproxHoriz);
                Draw(cmd, Pass.BlurVertical, Target.BlurVertical);
                Draw(cmd, Pass.BlurHorizontal, Target.BlurHorizontal);
                Draw(cmd, Pass.Deinterlace, Target.Deinterlace);
                Draw(cmd, Pass.FreezeFrame, Target.FreezeFrame, FullscreenVertices, true);
                Draw(cmd, Pass.GeneratePhosphorMask, Target.PhosphorMask, StripVertices, true);
                Draw(cmd, Pass.ApplyPhosphorMask, Target.MaskedScanlines);
                Draw(cmd, Pass.Brightpass, Target.Brightpass);
                Draw(cmd, Pass.BloomVertical, Target.BloomVertical);
                Draw(cmd, Pass.BloomHorizontal, Target.BloomHorizontal);
                Draw(cmd, Pass.Geometry, Target.Geometry);
                Draw(cmd, Pass.Uncrop, Target.Output, StripVertices);
            }

            cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_Material, (int)Pass.PresentOutput, MeshTopology.Triangles, FullscreenVertices);
            m_FrameCount = (m_FrameCount + 1) & 0x7FFFFFFF;
        }

        static bool Is8Bit(GraphicsFormat format)
        {
            switch (format)
            {
                case GraphicsFormat.R8G8B8A8_SRGB:
                case GraphicsFormat.B8G8R8A8_SRGB:
                case GraphicsFormat.R8G8B8A8_UNorm:
                case GraphicsFormat.B8G8R8A8_UNorm:
                    return true;
                default:
                    return false;
            }
        }

        void Draw(CommandBuffer cmd, Pass pass, Target target, int vertices = FullscreenVertices, bool keepContents = false)
        {
            var load = keepContents ? RenderBufferLoadAction.Load : RenderBufferLoadAction.DontCare;
            cmd.SetRenderTarget(m_Targets[(int)target], load, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, m_Material, (int)pass, MeshTopology.Triangles, vertices);
        }

        bool EnsureTargets(CommandBuffer cmd, int width, int height, CrtRoyaleSettings s)
        {
            var buffer = new Vector2Int(width, height);
            var content = s.useFullFrame
                ? buffer
                : new Vector2Int(Mathf.Clamp(s.contentSize.x, 1, width), Mathf.Clamp(s.contentSize.y, 1, height));

            if (m_Targets[0] != null && buffer == m_BufferSize && content == m_ContentSize
                && s.beamDistColorSamples == m_BeamColorSamples && s.beamDistDistanceSamples == m_BeamDistanceSamples
                && Mathf.Approximately(s.bloomApproxDownsizingFactor, m_BloomFactor))
                return false;

            ReleaseTargets();
            m_BufferSize = buffer;
            m_ContentSize = content;
            m_BeamColorSamples = s.beamDistColorSamples;
            m_BeamDistanceSamples = s.beamDistDistanceSamples;
            m_BloomFactor = s.bloomApproxDownsizingFactor;

            int smallWidth = Mathf.Max(1, (int)(content.x / m_BloomFactor));
            int smallHeight = Mathf.Max(1, (int)(content.y / m_BloomFactor));

            Create(Target.BackBuffer, buffer.x, buffer.y);
            Create(Target.Output, buffer.x, buffer.y);
            Create(Target.BeamDist, m_BeamColorSamples, m_BeamDistanceSamples, GraphicsFormat.A2B10G10R10_UNormPack32, TextureWrapMode.Repeat);
            Create(Target.BloomApproxVert, content.x, smallHeight);
            Create(Target.BloomApproxHoriz, smallWidth, smallHeight);
            Create(Target.BlurVertical, smallWidth, smallHeight);
            Create(Target.BlurHorizontal, smallWidth, smallHeight);
            foreach (var target in new[]
            {
                Target.PreblurVert, Target.PreblurHoriz, Target.ElectronBeams, Target.BeamConvergence, Target.Deinterlace,
                Target.FreezeFrame, Target.PhosphorMask, Target.MaskedScanlines, Target.Brightpass, Target.BloomVertical,
                Target.BloomHorizontal, Target.Geometry
            })
                Create(target, content.x, content.y);

            // The previous-frame texture is read before its first write.
            cmd.SetRenderTarget(m_Targets[(int)Target.FreezeFrame]);
            cmd.ClearRenderTarget(false, true, Color.clear);
            return true;
        }

        void Create(Target target, int width, int height, GraphicsFormat format = GraphicsFormat.R16G16B16A16_UNorm,
            TextureWrapMode wrapV = TextureWrapMode.Clamp)
        {
            var rt = new RenderTexture(width, height, 0, format)
            {
                name = "CrtRoyale_" + target,
                filterMode = FilterMode.Bilinear,
                wrapModeU = TextureWrapMode.Clamp,
                wrapModeV = wrapV,
                useMipMap = false,
                anisoLevel = 0,
                hideFlags = HideFlags.HideAndDontSave
            };
            rt.Create();
            m_Targets[(int)target] = rt;
        }

        void SetGlobals(CommandBuffer cmd, CrtRoyaleSettings s, bool refresh, bool sourceFlipY, bool destinationFlipY)
        {
            cmd.SetGlobalVector("buffer_size", (Vector2)m_BufferSize);
            cmd.SetGlobalVector("content_size", (Vector2)m_ContentSize);
            cmd.SetGlobalVector("content_center_offset", s.contentCenterOffset);
            cmd.SetGlobalFloat("num_beamdist_color_samples", m_BeamColorSamples);
            cmd.SetGlobalFloat("num_beamdist_dist_samples", m_BeamDistanceSamples);
            cmd.SetGlobalFloat("bloomapprox_downsizing_factor", m_BloomFactor);
            cmd.SetGlobalInteger("frame_count", (int)m_FrameCount);
            cmd.SetGlobalInteger("overlay_active", refresh ? 1 : 0);
            cmd.SetGlobalFloat("_CrtSourceFlipY", sourceFlipY ? 1f : 0f);
            cmd.SetGlobalFloat("_CrtDestFlipY", destinationFlipY ? 1f : 0f);
            cmd.SetGlobalFloat("_CrtLinearColorSpace", QualitySettings.activeColorSpace == ColorSpace.Linear ? 1f : 0f);

            cmd.SetGlobalFloat("content_box_inscribed", s.contentBoxInscribed ? 1f : 0f);
            cmd.SetGlobalFloat("content_box_thickness", s.contentBoxThickness);
            // The box is composited into the display-encoded image, so its colour must not be converted.
            cmd.SetGlobalVector("content_box_color", (Vector4)s.contentBoxColor);

            cmd.SetGlobalInteger("mask_type", (int)s.maskType);
            cmd.SetGlobalInteger("mask_size_param", (int)s.maskSizeParam);
            cmd.SetGlobalFloat("mask_triad_width", s.maskTriadWidth);
            cmd.SetGlobalFloat("mask_num_triads_across", s.maskNumTriadsAcross);
            cmd.SetGlobalFloat("scale_triad_height", s.scaleTriadHeight);
            cmd.SetGlobalVector("phosphor_thickness", s.phosphorThickness);
            cmd.SetGlobalVector("phosphor_sharpness", s.phosphorSharpness);
            cmd.SetGlobalVector("phosphor_offset_x", s.phosphorOffsetX);
            cmd.SetGlobalVector("phosphor_offset_y", s.phosphorOffsetY);

            cmd.SetGlobalInteger("scanline_thickness", Mathf.Max(1, s.scanlineThickness));
            cmd.SetGlobalFloat("scanline_offset", s.scanlineOffset);
            cmd.SetGlobalInteger("beam_shape_mode", (int)s.beamShape);
            cmd.SetGlobalInteger("enable_interlacing", s.enableInterlacing ? 1 : 0);
            cmd.SetGlobalInteger("interlace_back_field_first", s.interlaceBackFieldFirst ? 1 : 0);
            cmd.SetGlobalInteger("scanline_deinterlacing_mode", (int)s.deinterlacingMode);
            cmd.SetGlobalFloat("deinterlacing_blend_gamma", s.deinterlacingBlendGamma);
            cmd.SetGlobalFloat("linear_beam_thickness", s.linearBeamThickness);
            cmd.SetGlobalFloat("gaussian_beam_min_sigma", s.gaussianBeamMinSigma);
            cmd.SetGlobalFloat("gaussian_beam_max_sigma", s.gaussianBeamMaxSigma);
            cmd.SetGlobalFloat("gaussian_beam_spot_power", s.gaussianBeamSpotPower);
            cmd.SetGlobalFloat("gaussian_beam_min_shape", s.gaussianBeamMinShape);
            cmd.SetGlobalFloat("gaussian_beam_max_shape", s.gaussianBeamMaxShape);
            cmd.SetGlobalFloat("gaussian_beam_shape_power", s.gaussianBeamShapePower);
            cmd.SetGlobalVector("convergence_offset_x", s.convergenceOffsetX);
            cmd.SetGlobalVector("convergence_offset_y", s.convergenceOffsetY);

            cmd.SetGlobalFloat("crt_gamma", s.crtGamma);
            cmd.SetGlobalFloat("lcd_gamma", s.lcdGamma);
            cmd.SetGlobalFloat("levels_contrast", s.levelsContrast);
            cmd.SetGlobalFloat("halation_weight", s.halationWeight);
            cmd.SetGlobalFloat("diffusion_weight", s.diffusionWeight);
            cmd.SetGlobalFloat("blur_radius", s.blurRadius);
            cmd.SetGlobalFloat("bloom_underestimate_levels", s.bloomUnderestimateLevels);
            cmd.SetGlobalFloat("bloom_excess", s.bloomExcess);

            cmd.SetGlobalInteger("geom_rotation_mode", (int)s.rotation);
            cmd.SetGlobalInteger("geom_mode_runtime", (int)s.geometryMode);
            cmd.SetGlobalFloat("geom_radius", s.geometryRadius);
            cmd.SetGlobalFloat("geom_view_dist", s.viewDistance);
            cmd.SetGlobalVector("geom_tilt_angle", s.tiltAngle);
            cmd.SetGlobalVector("geom_aspect_ratio", s.aspectRatio);
            cmd.SetGlobalVector("geom_overscan", s.overscan);

            cmd.SetGlobalFloat("border_size", s.borderSize);
            cmd.SetGlobalFloat("border_darkness", s.borderDarkness);
            cmd.SetGlobalFloat("border_compress", s.borderCompress);

            cmd.SetGlobalVector("preblur_effect_radius", s.preblurEffectRadius);
            cmd.SetGlobalVector("preblur_sampling_radius", (Vector2)s.preblurSamplingRadius);

            cmd.SetGlobalFloat("antialias_level", s.antialiasLevel);
            cmd.SetGlobalFloat("aa_filter", (float)s.antialiasFilter);
            cmd.SetGlobalInteger("aa_temporal", s.antialiasTemporal ? 1 : 0);
        }

        void ReleaseTargets()
        {
            for (int i = 0; i < m_Targets.Length; i++)
            {
                if (m_Targets[i] == null) continue;
                m_Targets[i].Release();
                DestroyObject(m_Targets[i]);
                m_Targets[i] = null;
            }
        }

        static void DestroyObject(UnityEngine.Object obj)
        {
            if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
            else UnityEngine.Object.DestroyImmediate(obj);
        }

        public void Dispose()
        {
            ReleaseTargets();
            DestroyObject(m_Material);
        }
    }
}
