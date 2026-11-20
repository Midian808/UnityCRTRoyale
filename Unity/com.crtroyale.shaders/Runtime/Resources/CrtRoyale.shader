Shader "Hidden/CrtRoyale"
{
    HLSLINCLUDE
    #pragma target 5.0
    #pragma only_renderers d3d11 vulkan
    #include "Packages/com.crtroyale.shaders/Runtime/Shaders/Core/crt-royale-unity.hlsl"
    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always Blend Off

        Pass { Name "CaptureInput"        HLSLPROGRAM
            #pragma vertex PostProcessVS
            #pragma fragment captureInputPS
        ENDHLSL }
        Pass { Name "ContentBox"          HLSLPROGRAM
            #pragma vertex PostProcessVS
            #pragma fragment contentBoxPixelShader
        ENDHLSL }
        Pass { Name "PreblurVert"         HLSLPROGRAM
            #pragma vertex contentCropVS
            #pragma fragment preblurVertPS
        ENDHLSL }
        Pass { Name "PreblurHoriz"        HLSLPROGRAM
            #pragma vertex PostProcessVS
            #pragma fragment preblurHorizPS
        ENDHLSL }
        Pass { Name "BeamDist"            HLSLPROGRAM
            #pragma vertex calculateBeamDistsVS
            #pragma fragment calculateBeamDistsPS
        ENDHLSL }
        Pass { Name "ElectronBeam"        HLSLPROGRAM
            #pragma multi_compile_local _ CRT_PREBLUR_OFF
            #pragma vertex simulateEletronBeamsVS
            #pragma fragment simulateEletronBeamsPS
        ENDHLSL }
        Pass { Name "BeamConvergence"     HLSLPROGRAM
            #pragma vertex beamConvergenceVS
            #pragma fragment beamConvergencePS
        ENDHLSL }
        Pass { Name "BloomApproxVert"     HLSLPROGRAM
            #pragma vertex PostProcessVS
            #pragma fragment approximateBloomVertPS
        ENDHLSL }
        Pass { Name "BloomApproxHoriz"    HLSLPROGRAM
            #pragma vertex PostProcessVS
            #pragma fragment approximateBloomHorizPS
        ENDHLSL }
        Pass { Name "BlurVertical"        HLSLPROGRAM
            #pragma vertex blurVerticalVS
            #pragma fragment blurVerticalPS
        ENDHLSL }
        Pass { Name "BlurHorizontal"      HLSLPROGRAM
            #pragma vertex blurHorizontalVS
            #pragma fragment blurHorizontalPS
        ENDHLSL }
        Pass { Name "Deinterlace"         HLSLPROGRAM
            #pragma vertex deinterlaceVS
            #pragma fragment deinterlacePS
        ENDHLSL }
        Pass { Name "FreezeFrame"         HLSLPROGRAM
            #pragma vertex freezeFrameVS
            #pragma fragment freezeFramePS
        ENDHLSL }
        Pass { Name "GeneratePhosphorMask" HLSLPROGRAM
            #pragma vertex generatePhosphorMaskVS
            #pragma fragment generatePhosphorMaskPS
        ENDHLSL }
        Pass { Name "ApplyPhosphorMask"   HLSLPROGRAM
            #pragma vertex PostProcessVS
            #pragma fragment applyComputedPhosphorMaskPS
        ENDHLSL }
        Pass { Name "Brightpass"          HLSLPROGRAM
            #pragma vertex brightpassVS
            #pragma fragment brightpassPS
        ENDHLSL }
        Pass { Name "BloomVertical"       HLSLPROGRAM
            #pragma vertex bloomVerticalVS
            #pragma fragment bloomVerticalPS
        ENDHLSL }
        Pass { Name "BloomHorizontal"     HLSLPROGRAM
            #pragma vertex bloomHorizontalVS
            #pragma fragment bloomHorizontalPS
        ENDHLSL }
        Pass { Name "Geometry"            HLSLPROGRAM
            #pragma vertex geometryVS
            #pragma fragment geometryPS
        ENDHLSL }
        Pass { Name "Uncrop"              HLSLPROGRAM
            #pragma vertex contentUncropVS
            #pragma fragment uncropContentPixelShader
        ENDHLSL }
        Pass { Name "PresentOutput"       HLSLPROGRAM
            #pragma vertex PostProcessVS
            #pragma fragment presentOutputPS
        ENDHLSL }
    }
}
