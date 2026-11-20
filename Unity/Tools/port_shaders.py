"""Generates the Unity HLSL core from the ReShade crt-royale sources.

Usage: python -I port_shaders.py <Shaders folder> <package Core output folder>
Only ReShade-specific constructs change; the shader math stays as written upstream.
"""
import pathlib
import re
import sys

SRC = pathlib.Path(sys.argv[1]) / "crt-royale"
OUT = pathlib.Path(sys.argv[2])


def must_sub(pattern, repl, text, count=0, flags=0):
    new, n = re.subn(pattern, repl, text, count=count, flags=flags)
    if n == 0:
        raise SystemExit(f"pattern did not match: {pattern}")
    return new


def strip_function_statics(text):
    """HLSL rejects non-constant static locals; ReShade evaluates them per call."""
    out, depth, in_block = [], 0, False
    for line in text.split("\n"):
        code = line
        if in_block:
            if "*/" in code:
                code = code.split("*/", 1)[1]
                in_block = False
            else:
                code = ""
        code = re.sub(r"/\*.*?\*/", "", code)
        if "/*" in code:
            code = code.split("/*", 1)[0]
            in_block = True
        code = code.split("//", 1)[0]
        if depth > 0 and not line.lstrip().startswith("//"):
            line = re.sub(r"\bstatic\s+const\b", "const", line)
        depth += code.count("{") - code.count("}")
        out.append(line)
    return "\n".join(out)


def common(text):
    text = re.sub(r'(#include\s+"[^"]*)\.fxh"', r'\1.hlsl"', text)
    text = strip_function_statics(text)
    text = re.sub(r"\bconst\s+sampler2D\b", "sampler2D", text)
    text = re.sub(r"\bin\s+const\b", "in", text)
    text = re.sub(r"(tex2Dlod\(.*\)),\s*0\.0\)", r"\1)", text)
    return text.replace("ReShade::BackBuffer", "samplerBackBuffer")


def bind_params(text):
    text = re.sub(r"/\*\s*\n// ==== PIXELATION.*?\*/", "", text, flags=re.S)
    head = """//  Unity supplies these at runtime; ReShade fixed them at compile time.
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

"""
    text = must_sub(
        r"//  Override some parameters for gamma-management.*?(?=static const float gba_gamma)",
        lambda _: head,
        text,
        count=1,
        flags=re.S,
    )
    text = re.sub(r"uniform int (APPEND_VERSION_SUFFIX\(version\)|basic_setup_help|content_box_help)\s*<.*?>;\s*", "", text, flags=re.S)

    def uniform(match):
        kind, name = match.group(1), match.group(2)
        kind = {"bool": "int", "uint2": "float2"}.get(kind, kind)
        return f"uniform {kind} {name};"

    return re.sub(r"uniform\s+(\w+)\s+(\w+)\s*<(?:(?!>\s*(?:=|;)).)*>\s*(?:=[^;]*)?;", uniform, text, flags=re.S)


def shared_objects(text):
    text = re.sub(r"/\*.*?\*/", "", text, flags=re.S)
    text = re.sub(r"(?m)^\s*//.*$", "", text)
    text = re.sub(r"(?m)^\s*static const uint2 TEX_PREBLUR_SIZE.*$", "", text)
    text = re.sub(r"(?s)texture2D\s+\w+\s*<[^>]*>\s*\{.*?\};", "", text)
    text = re.sub(r"(?s)sampler2D\s+(\w+)\s*\{.*?\};", r"sampler2D \1;", text)
    text = text.replace("int2(TEX_FREEZEFRAME_WIDTH, TEX_FREEZEFRAME_HEIGHT\n", "int2(TEX_FREEZEFRAME_WIDTH, TEX_FREEZEFRAME_HEIGHT)\n")
    return text.replace("#endif  // _SHARED_OBJECTS_H", "sampler2D samplerBackBuffer;\n\n#endif  // _SHARED_OBJECTS_H")


def user_settings(text):
    text = must_sub(
        r"#ifndef antialias_level\s+#define antialias_level 0\.0\s+#endif",
        "uniform float antialias_level;",
        text,
    )
    text = must_sub(
        r"#ifndef antialias_filter\s+#define antialias_filter 6\s+#endif\s+static const float aa_filter = antialias_filter;[^\n]*",
        "uniform float aa_filter;",
        text,
    )
    return must_sub(
        r"#ifndef antialias_temporal\s+#define antialias_temporal false\s+#endif\s+static const bool aa_temporal = antialias_temporal;",
        "uniform int aa_temporal;",
        text,
    )


def antialias(text):
    text = must_sub(
        r"static const float2 aa_xy_axis_importance = macro_cond\(.*?\n    \);",
        lambda _: """#define aa_xy_axis_importance (aa_filter < 5.5 ? float2(1.0, 1.0) : \\
        aa_filter < 8.5 ? float2(1.0, 0.0) : \\
        aa_filter < 9.5 ? float2(1.0, 1.0/aa_lanczos_lobes) : float2(1.0, 1.0))""",
        text,
        count=1,
        flags=re.S,
    )
    levels = [
        ("0.5", "tex2D_linearize(tex, tex_uv, input_gamma).rgb"),
        ("3.5", "tex2Daa_subpixel_weights_only(tex, tex_uv, pixel_to_tex_uv, input_gamma)"),
    ]
    for limit, fn in [("4.5", "tex2Daa4x"), ("5.5", "tex2Daa5x"), ("6.5", "tex2Daa6x"), ("7.5", "tex2Daa7x"),
                      ("11.5", "tex2Daa8x"), ("15.5", "tex2Daa12x"), ("19.5", "tex2Daa16x"), ("23.5", "tex2Daa20x"),
                      ("253.5", "tex2Daa24x"), ("254.5", "tex2Daa_debug_16x_regular")]:
        levels.append((limit, f"{fn}(tex, tex_uv, pixel_to_tex_uv, frame, input_gamma)"))
    body = "    //  antialias_level is a runtime setting, so only the selected level may run:\n"
    for limit, call in levels:
        body += f"    [branch] if (antialias_level < {limit}) return {call};\n"
    body += "    return tex2Daa_debug_dynamic(tex, tex_uv, pixel_to_tex_uv, frame, input_gamma);\n"
    return must_sub(r"    //  Statically switch between antialiasing modes/levels:\n.*?(?=\n\}\s*\n\s*\n#endif)", lambda _: body.rstrip("\n"), text, count=1, flags=re.S)


def content_box(text):
    box = """//  The debug box is a runtime pass here, so its options are uniforms.
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
"""
    text = must_sub(r"#if CONTENT_BOX_VISIBLE.*(?=#endif  //  _CONTENT_BOX_H)", lambda _: box + "\n", text, count=1, flags=re.S)
    return add_strip_remap(text, ["contentCropVS", "contentUncropVS"])


def add_strip_remap(text, functions):
    """Unity has no triangle-strip topology, so these passes draw six indexed vertices."""
    for name in functions:
        text = must_sub(rf"(void {name}\(.*?\)\s*\{{\n)", r"\1" + "    id = crt_strip_vertex_id(id);\n", text, flags=re.S)
    return text


def scanlines(text):
    # precalc_interpolation_field_data leaves fields unset, which fxc rejects when the struct is returned.
    text = must_sub(r"InterpolationFieldData data;", "InterpolationFieldData data = (InterpolationFieldData)0;", text)
    return must_sub(r"const float4 sample_([rgb]) = tex2D\(tex, scanline_uv_\1\);", r"const float4 sample_\1 = tex2D_border(tex, scanline_uv_\1);", text)


def electron_beams(text):
    return must_sub(r"tex2D\(samplerElectronBeams,", "tex2D_border(samplerElectronBeams,", text)


def geometry_last_pass(text):
    return must_sub(r"(\n\s*)if\(antialias_level > 0\.5 && \(geom_mode", r"\1[branch] if(antialias_level > 0.5 && (geom_mode", text)


SPECIAL = {
    "lib/bind-shader-params.fxh": bind_params,
    "lib/user-settings.fxh": user_settings,
    "lib/tex2Dantialias.fxh": antialias,
    "lib/scanline-functions.fxh": scanlines,
    "shaders/shared-objects.fxh": shared_objects,
    "shaders/content-box.fxh": content_box,
    "shaders/electron-beams.fxh": electron_beams,
    "shaders/phosphor-mask.fxh": lambda t: add_strip_remap(t, ["generatePhosphorMaskVS"]),
    "shaders/geometry-aa-last-pass.fxh": geometry_last_pass,
}

for path in sorted(SRC.rglob("*.fxh")):
    rel = path.relative_to(SRC).as_posix()
    text = path.read_text(encoding="utf-8", errors="strict").replace("\r\n", "\n")
    if rel in SPECIAL:
        text = SPECIAL[rel](text)
    text = common(text)
    target = OUT / (rel[:-4] + ".hlsl")
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(text, encoding="utf-8", newline="\n")
    print(rel)
