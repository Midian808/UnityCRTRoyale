#ifndef _BIND_SHADER_PARAMS_H
#define _BIND_SHADER_PARAMS_H

/////////////////////////////  GPL LICENSE NOTICE  /////////////////////////////

//  crt-royale: A full-featured CRT shader, with cheese.
//  Copyright (C) 2014 TroggleMonkey <trogglemonkey@gmx.com>
//
//  crt-royale-reshade: A port of TroggleMonkey's crt-royale from libretro to ReShade.
//  Copyright (C) 2020 Alex Gunter <akg7634@gmail.com>
//
//  This program is free software; you can redistribute it and/or modify it
//  under the terms of the GNU General Public License as published by the Free
//  Software Foundation; either version 2 of the License, or any later version.
//
//  This program is distributed in the hope that it will be useful, but WITHOUT
//  ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
//  FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for
//  more details.
//
//  You should have received a copy of the GNU General Public License along with
//  this program; if not, write to the Free Software Foundation, Inc., 59 Temple
//  Place, Suite 330, Boston, MA 02111-1307 USA


/////////////////////////////  SETTINGS MANAGEMENT  ////////////////////////////

///////////////////////////////  BEGIN INCLUDES  ///////////////////////////////
#include "helper-functions-and-macros.hlsl"
#include "user-settings.hlsl"
#include "derived-settings-and-constants.hlsl"
#include "../version-number.hlsl"

////////////////////////////////  END INCLUDES  ////////////////////////////////

//  Unity supplies these at runtime; ReShade fixed them at compile time.
#define _OVERRIDE_DEVICE_GAMMA 1
#define _DX9_ACTIVE 0

#ifdef CRT_PREBLUR_OFF
    #define ENABLE_PREBLUR 0
#else
    #define ENABLE_PREBLUR 1
#endif

uniform float2 buffer_size;
uniform float2 content_size;
uniform float2 content_center_offset;
uniform float num_beamdist_color_samples;
uniform float num_beamdist_dist_samples;
uniform float bloomapprox_downsizing_factor;

#define BUFFER_WIDTH (buffer_size.x)
#define BUFFER_HEIGHT (buffer_size.y)
#define CONTENT_WIDTH (content_size.x)
#define CONTENT_HEIGHT (content_size.y)
#define content_center (content_center_offset / buffer_size + 0.5)
#define content_radius (content_size / (2.0 * buffer_size))
#define content_scale (content_size / buffer_size)
#define content_left (content_center.x - content_radius.x)
#define content_right (content_center.x + content_radius.x)
#define content_upper (content_center.y - content_radius.y)
#define content_lower (content_center.y + content_radius.y)
#define content_offset float2(content_left, content_upper)

uniform uint frame_count;
uniform int overlay_active;

static const float gba_gamma = 3.5; //  Irrelevant but necessary to define.


// === HELP AND INFO ===

// ==== PHOSPHOR MASK ====
uniform int mask_type;

uniform uint mask_size_param;

uniform float mask_triad_width;

uniform float mask_num_triads_across;

uniform float scale_triad_height;

uniform float2 phosphor_thickness;

uniform float2 phosphor_sharpness;

uniform float3 phosphor_offset_x;

uniform float3 phosphor_offset_y;

// static const uint pixel_grid_mode = 0;
// static const float2 pixel_size = 1;


// ==== SCANLINES ====
uniform uint scanline_thickness;

uniform float scanline_offset;

uniform uint beam_shape_mode;

uniform int enable_interlacing;

uniform int interlace_back_field_first;

uniform uint scanline_deinterlacing_mode;

uniform float deinterlacing_blend_gamma;

uniform float linear_beam_thickness;

uniform float gaussian_beam_min_sigma;

uniform float gaussian_beam_max_sigma;

uniform float gaussian_beam_spot_power;

uniform float gaussian_beam_min_shape;

uniform float gaussian_beam_max_shape;

uniform float gaussian_beam_shape_power;

uniform float3 convergence_offset_x;
uniform float3 convergence_offset_y;

static uint beam_horiz_filter = beam_horiz_filter_static;
static float beam_horiz_sigma = beam_horiz_sigma_static;
static float beam_horiz_linear_rgb_weight = beam_horiz_linear_rgb_weight_static;

// ==== IMAGE COLORIZATION ====
uniform float crt_gamma;

uniform float lcd_gamma;

uniform float levels_contrast;

uniform float halation_weight;

uniform float diffusion_weight;

uniform float blur_radius;

uniform float bloom_underestimate_levels;

uniform float bloom_excess;

uniform float2 aa_subpixel_r_offset_runtime;

static const float aa_cubic_c = aa_cubic_c_static;
static const float aa_gauss_sigma = aa_gauss_sigma_static;


// ==== GEOMETRY ====
uniform uint geom_rotation_mode;
uniform uint geom_mode_runtime;

uniform float geom_radius;

uniform float geom_view_dist;

uniform float2 geom_tilt_angle;

uniform float2 geom_aspect_ratio;
uniform float2 geom_overscan;

// ==== BORDER ====
uniform float border_size;

uniform float border_darkness;

uniform float border_compress;

// ==== PREBLUR ====
#if ENABLE_PREBLUR
    uniform float2 preblur_effect_radius;
    uniform float2 preblur_sampling_radius;
#else
    static const float2 preblur_effect_radius = 0;
    static const uint2 preblur_sampling_radius = 0;
#endif

//  Provide accessors for vector constants that pack scalar uniforms:
float2 get_aspect_vector(const float geom_aspect_ratio)
{
    //  Get an aspect ratio vector.  Enforce geom_max_aspect_ratio, and prevent
    //  the absolute scale from affecting the uv-mapping for curvature:
    const float geom_clamped_aspect_ratio =
        min(geom_aspect_ratio, geom_max_aspect_ratio);
    const float2 geom_aspect =
        normalize(float2(geom_clamped_aspect_ratio, 1.0));
    return geom_aspect;
}

float2 get_geom_overscan_vector()
{
    return geom_overscan;
}

float2 get_geom_tilt_angle_vector()
{
    return geom_tilt_angle;
}

float3 get_convergence_offsets_x_vector()
{
    return convergence_offset_x;
}

float3 get_convergence_offsets_y_vector()
{
    return convergence_offset_y;
}

float2 get_convergence_offsets_r_vector()
{
    return float2(convergence_offset_x.r, convergence_offset_y.r);
}

float2 get_convergence_offsets_g_vector()
{
    return float2(convergence_offset_x.g, convergence_offset_y.g);
}

float2 get_convergence_offsets_b_vector()
{
    return float2(convergence_offset_x.b, convergence_offset_y.b);
}

float2 get_aa_subpixel_r_offset()
{
    #if _RUNTIME_ANTIALIAS_WEIGHTS
        #if _RUNTIME_ANTIALIAS_SUBPIXEL_OFFSETS
            //  WARNING: THIS IS EXTREMELY EXPENSIVE.
            return aa_subpixel_r_offset_runtime;
        #else
            return aa_subpixel_r_offset_static;
        #endif
    #else
        return aa_subpixel_r_offset_static;
    #endif
}

//  Provide accessors settings which still need "cooking:"
float get_mask_amplify()
{
    const float mask_grille_amplify = 1.0/mask_grille_avg_color;
    const float mask_slot_amplify = 1.0/mask_slot_avg_color;
    const float mask_shadow_amplify = 1.0/mask_shadow_avg_color;

    float mask_amplify;
    [flatten]
    switch (mask_type) {
        case 0:
            mask_amplify = mask_grille_amplify;
            break;
        case 1:
            mask_amplify = mask_slot_amplify;
            break;
        case 2:
            mask_amplify = mask_shadow_amplify;
            break;
        case 3:
            mask_amplify = mask_grille_amplify;
            break;
        case 4:
            mask_amplify = mask_slot_amplify;
            break;
        default:
            mask_amplify = mask_shadow_amplify;
            break;
                    
    }
    
    return mask_amplify;
}

#endif  //  _BIND_SHADER_PARAMS_H