using System;
using UnityEngine;

namespace CrtRoyale
{
    public enum MaskType { Grille, Slot, Shadow, LowResGrille, LowResSlot, LowResShadow }
    public enum MaskSizeParam { TriadWidth, NumTriadsAcross }
    public enum BeamShape { Digital, Linear, Gaussian, MultiSourceGaussian }
    public enum DeinterlacingMode { None, FakeProgressive, Weaving, BlendedWeaving }
    public enum ScreenRotation { Degrees0, Degrees90, Degrees180, Degrees270 }
    public enum GeometryMode { Flat, Spherical, SphericalAlt, Cylindrical }
    public enum AntialiasFilter { BoxSeparable, BoxCylindrical, TentSeparable, TentCylindrical, GaussianSeparable, GaussianCylindrical, CubicSeparable, CubicCylindrical, LanczosSinc, LanczosJinc }

    /// <summary>Every crt-royale-reshade setting, with that shader's default values.</summary>
    [Serializable]
    public sealed class CrtRoyaleSettings
    {
        [Header("Content Box")]
        [Tooltip("Use the whole camera image. Turn off to crop to Content Size, as for letter-boxed content.")]
        public bool useFullFrame = true;
        public Vector2Int contentSize = new Vector2Int(1920, 1080);
        [Tooltip("Offset of the content's centre from the image centre, in pixels.")]
        public Vector2 contentCenterOffset = Vector2.zero;
        [Tooltip("Draws the crop box over the unprocessed image, to help align it.")]
        public bool showContentBox = false;
        public bool contentBoxInscribed = true;
        [Min(0f)] public float contentBoxThickness = 5f;
        public Color contentBoxColor = Color.red;

        [Header("Phosphor Mask")]
        public MaskType maskType = MaskType.LowResSlot;
        public MaskSizeParam maskSizeParam = MaskSizeParam.TriadWidth;
        [Range(1f, 60f)] public float maskTriadWidth = 3f;
        [Range(1f, 1280f)] public float maskNumTriadsAcross = 480f;
        [Range(0.01f, 10f)] public float scaleTriadHeight = 1f;
        public Vector2 phosphorThickness = new Vector2(0.2f, 0.2f);
        public Vector2 phosphorSharpness = new Vector2(50f, 50f);
        public Vector3 phosphorOffsetX = Vector3.zero;
        public Vector3 phosphorOffsetY = Vector3.zero;

        [Header("Scanlines")]
        [Range(1, 30)] public int scanlineThickness = 2;
        [Range(-30f, 30f)] public float scanlineOffset = 0f;
        public BeamShape beamShape = BeamShape.Linear;
        public bool enableInterlacing = false;
        public bool interlaceBackFieldFirst = false;
        public DeinterlacingMode deinterlacingMode = DeinterlacingMode.FakeProgressive;
        [Range(0.01f, 5f)] public float deinterlacingBlendGamma = 1f;
        [Range(0.01f, 3f)] public float linearBeamThickness = 1f;
        [Min(0f)] public float gaussianBeamMinSigma = 0.02f;
        [Min(0f)] public float gaussianBeamMaxSigma = 0.3f;
        [Min(0f)] public float gaussianBeamSpotPower = 1f / 3f;
        [Min(0f)] public float gaussianBeamMinShape = 2f;
        [Min(0f)] public float gaussianBeamMaxShape = 4f;
        [Min(0f)] public float gaussianBeamShapePower = 0.25f;
        public Vector3 convergenceOffsetX = Vector3.zero;
        public Vector3 convergenceOffsetY = Vector3.zero;

        [Header("Colors and Effects")]
        [Range(1f, 5f)] public float crtGamma = 2.5f;
        [Range(1f, 5f)] public float lcdGamma = 2.2f;
        [Range(0f, 4f)] public float levelsContrast = 1f;
        [Range(0f, 1f)] public float halationWeight = 0f;
        [Range(0f, 1f)] public float diffusionWeight = 0.075f;
        [Range(0.01f, 5f)] public float blurRadius = 1f;
        [Min(0.0000152587890625f)] public float bloomUnderestimateLevels = 0.8f;
        [Range(0f, 1f)] public float bloomExcess = 0f;

        [Header("Screen Geometry")]
        public ScreenRotation rotation = ScreenRotation.Degrees0;
        public GeometryMode geometryMode = GeometryMode.Flat;
        [Range(0.1591549f, 1024f)] public float geometryRadius = 2f;
        [Range(0.5f, 1024f)] public float viewDistance = 2f;
        public Vector2 tiltAngle = Vector2.zero;
        public Vector2 aspectRatio = new Vector2(1.313069909f, 1f);
        public Vector2 overscan = Vector2.one;

        [Header("Screen Border")]
        [Range(0f, 0.5f)] public float borderSize = 0.015f;
        [Min(0f)] public float borderDarkness = 2f;
        [Min(0f)] public float borderCompress = 2.5f;

        [Header("Pre-Blur")]
        [Tooltip("Runs the two pre-blur passes. Leave the radii at 0 and turn this off to save their cost.")]
        public bool enablePreblur = true;
        public Vector2 preblurEffectRadius = Vector2.zero;
        public Vector2Int preblurSamplingRadius = Vector2Int.zero;

        [Header("Antialiasing")]
        [Tooltip("Samples per pixel for curved or overscanned output: 0, 1, 4, 5, 6, 7, 8, 12, 16, 20 or 24.")]
        [Range(0, 24)] public int antialiasLevel = 0;
        public AntialiasFilter antialiasFilter = AntialiasFilter.CubicSeparable;
        public bool antialiasTemporal = false;

        [Header("Advanced")]
        [Min(2)] public int beamDistColorSamples = 1024;
        [Min(2)] public int beamDistDistanceSamples = 120;
        [Min(1f)] public float bloomApproxDownsizingFactor = 4f;

        public Hash128 ComputeHash() => Hash128.Compute(JsonUtility.ToJson(this));
    }
}
