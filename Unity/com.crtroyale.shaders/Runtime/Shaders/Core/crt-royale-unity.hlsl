#ifndef _CRT_ROYALE_UNITY_H
#define _CRT_ROYALE_UNITY_H

//  crt-royale-reshade: A port of TroggleMonkey's crt-royale from libretro to ReShade.
//  Copyright (C) 2014 TroggleMonkey, 2020 Alex Gunter.
//  This Unity port is distributed under the GNU GPL, version 3 or later (see LICENSE).

//  No render target has mipmaps, so every lookup uses LOD 0. That also keeps
//  lookups legal inside the dynamic branches and loops the passes rely on.
#define tex2D(tex, coord) tex2Dlod(tex, float4((coord).xy, 0, 0))

//  Unity has no border address mode; this returns the black border ReShade used.
#define tex2D_border(tex, coord) (tex2D(tex, coord) * float(all((coord).xy >= 0.0) && all((coord).xy <= 1.0)))

//  Fullscreen triangle matching ReShade's PostProcessVS.
void PostProcessVS(in uint id : SV_VertexID, out float4 position : SV_Position, out float2 texcoord : TEXCOORD0)
{
    texcoord.x = (id == 2) ? 2.0 : 0.0;
    texcoord.y = (id == 1) ? 2.0 : 0.0;
    position = float4(texcoord * float2(2.0, -2.0) + float2(-1.0, 1.0), 0.0, 1.0);
}

//  Maps six triangle-list vertices onto the four vertices of a ReShade strip.
uint crt_strip_vertex_id(uint id)
{
    return (id < 3) ? id : ((id == 3) ? 2 : ((id == 4) ? 1 : 3));
}

#include "shaders/content-box.hlsl"
#include "shaders/input-blurring.hlsl"
#include "shaders/electron-beams.hlsl"
#include "shaders/blurring.hlsl"
#include "shaders/deinterlace.hlsl"
#include "shaders/phosphor-mask.hlsl"
#include "shaders/brightpass.hlsl"
#include "shaders/bloom.hlsl"
#include "shaders/geometry-aa-last-pass.hlsl"

//  Input and output adapters between Unity's camera target and the display-encoded,
//  top-down image the ReShade passes expect from the back buffer.
sampler2D _CrtSource;
sampler2D _CrtOutput;
uniform float _CrtSourceFlipY;
uniform float _CrtDestFlipY;
uniform float _CrtLinearColorSpace;
uniform float _CrtSourceIs8Bit;

float3 crt_linear_to_srgb(float3 c)
{
    return (c <= 0.0031308) * (c * 12.92) + (c > 0.0031308) * (1.055 * pow(max(c, 0.0031308), 1.0 / 2.4) - 0.055);
}

float3 crt_srgb_to_linear(float3 c)
{
    return (c <= 0.04045) * (c / 12.92) + (c > 0.04045) * pow(max((c + 0.055) / 1.055, 0.04045), 2.4);
}

void captureInputPS(in float4 pos : SV_Position, in float2 texcoord : TEXCOORD0, out float4 color : SV_Target)
{
    const float2 uv = float2(texcoord.x, lerp(texcoord.y, 1.0 - texcoord.y, _CrtSourceFlipY));
    const float3 source = saturate(tex2D(_CrtSource, uv).rgb);
    float3 encoded = _CrtLinearColorSpace > 0.5 ? crt_linear_to_srgb(source) : source;
    //  An 8-bit source holds exact 1/255 steps; this removes the error of decoding and re-encoding them.
    if (_CrtSourceIs8Bit > 0.5)
        encoded = round(encoded * 255.0) / 255.0;
    color = float4(encoded, 1.0);
}

void presentOutputPS(in float4 pos : SV_Position, in float2 texcoord : TEXCOORD0, out float4 color : SV_Target)
{
    const float2 uv = float2(texcoord.x, lerp(texcoord.y, 1.0 - texcoord.y, _CrtDestFlipY));
    const float3 encoded = tex2D(_CrtOutput, uv).rgb;
    color = float4(_CrtLinearColorSpace > 0.5 ? crt_srgb_to_linear(encoded) : encoded, 1.0);
}

#endif  //  _CRT_ROYALE_UNITY_H
