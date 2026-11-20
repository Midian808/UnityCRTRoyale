#ifndef _CONTENT_BOX_H
#define _CONTENT_BOX_H

/////////////////////////////////  MIT LICENSE  ////////////////////////////////

//  Copyright (C) 2020 Alex Gunter
//
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to
//  deal in the Software without restriction, including without limitation the
//  rights to use, copy, modify, merge, publish, distribute, sublicense, and/or
//  sell copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
//  
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
//
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
//  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
//  IN THE SOFTWARE.


#include "shared-objects.hlsl"


void contentCropVS(
    in uint id : SV_VertexID,

    out float4 position : SV_Position,
    out float2 texcoord : TEXCOORD0
) {
    id = crt_strip_vertex_id(id);
    #if _DX9_ACTIVE
        texcoord.x = (id == 1 || id == 3) ? content_right : content_left;
        texcoord.y = (id > 1) ? content_lower : content_upper;

        position.x = (id == 1 || id == 3) ?  1 : -1;
        position.y = (id > 1) ? -1 :  1;
        position.zw = 1;
    #else
        texcoord.x = (id & 1) ? content_right : content_left;
        texcoord.y = (id & 2) ? content_lower : content_upper;

        position.x = (id & 1) ?  1 : -1;
        position.y = (id & 2) ? -1 :  1;
        position.zw = 1;
    #endif
}

#if USE_VERTEX_UNCROPPING
/*
 *  Using the vertex shader for uncropping can save about 0.1ms in some apps.
 *  However, some apps like SNES9X w/ DX9 don't trigger a refresh of the entire screen,
 *  which in turn causes the ReShade UI to "stick around" after it's closed.
 *  
 *  The slower algorithm forces the entire screen to refresh, which forces the
 *  area outside the content box to be black. I assume most users will prefer
 *  the results of the slower algorithm and won't notice the 0.1ms. Users who
 *  need that 0.1ms can use a preprocessor def to recover that time.
 */
    void contentUncropVS(
        in uint id : SV_VertexID,

        out float4 position : SV_Position,
        out float2 texcoord : TEXCOORD0
    ) {
    id = crt_strip_vertex_id(id);
        #if _DX9_ACTIVE
            texcoord.x = id == 1 || id == 3;
            texcoord.y = id < 2;

            position.x = (id == 1 || id == 3) ? content_scale.x : -content_scale.x;
            position.y = (id > 1) ? content_scale.y : -content_scale.y;
            position.zw = 1;
        #else
            texcoord.x = id & 1;
            texcoord.y = !(id & 2);
            
            position.x = (id & 1) ? content_scale.x : -content_scale.x;
            position.y = (id & 2) ? content_scale.y : -content_scale.y;
            position.zw = 1;
        #endif
    }

    void uncropContentPixelShader(
        in float4 pos : SV_Position,
        in float2 texcoord : TEXCOORD0,

        out float4 color : SV_Target
    ) {
        color = tex2D(samplerGeometry, texcoord);
    }
#else
    void contentUncropVS(
        in uint id : SV_VertexID,

        out float4 position : SV_Position,
        out float2 texcoord : TEXCOORD0
    ) {
    id = crt_strip_vertex_id(id);
        // TODO: There's probably a better way to code this.
        // I'll figure it out later.
        #if _DX9_ACTIVE
            texcoord.x = id == 1 || id == 3;
            texcoord.y = id < 2;

            position.x = (id == 1 || id == 3) ? 1 : -1;
            position.y = (id > 1) ? 1 : -1;
            position.zw = 1;
        #else
            texcoord.x = id & 1;
            texcoord.y = !(id & 2);
            
            position.x = (id & 1) ? 1 : -1;
            position.y = (id & 2) ? 1 : -1;
            position.zw = 1;
        #endif
    }

    void uncropContentPixelShader(
        in float4 pos : SV_Position,
        in float2 texcoord : TEXCOORD0,

        out float4 color : SV_Target
    ) {
        const bool is_in_boundary = float(
            texcoord.x >= content_left && texcoord.x <= content_right &&
            texcoord.y >= content_upper && texcoord.y <= content_lower
        );
        const float2 texcoord_uncropped = ((texcoord - content_offset) * buffer_size + 0) / content_size;

        const float4 raw_color = tex2D(samplerGeometry, texcoord_uncropped);
        color = float4(is_in_boundary * raw_color.rgb, raw_color.a);
    }
#endif


//  The debug box is a runtime pass here, so its options are uniforms.
uniform float content_box_inscribed;
uniform float content_box_thickness;
uniform float4 content_box_color;

void contentBoxPixelShader(
    in float4 pos : SV_Position,
    in float2 texcoord : TEXCOORD0,

    out float4 color : SV_Target
) {
    const float2 thickness = content_box_thickness / buffer_size;
    const float2 outward = (1.0 - content_box_inscribed) * thickness;
    const float2 inward = content_box_inscribed * thickness;
    const float2 box_min = float2(content_left, content_upper);
    const float2 box_max = float2(content_right, content_lower);

    const bool is_inside_outerbound = all(texcoord >= box_min - outward) && all(texcoord <= box_max + outward);
    const bool is_inside_innerbound = all(texcoord > box_min + inward) && all(texcoord < box_max - inward);

    color = (is_inside_outerbound && !is_inside_innerbound) ? content_box_color : tex2D(samplerBackBuffer, texcoord);
}

#endif  //  _CONTENT_BOX_H