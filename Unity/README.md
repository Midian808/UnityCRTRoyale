# CRT Royale for Unity 6

A port of [crt-royale-reshade](https://github.com/akgunter/crt-royale-reshade) 2.1.0 as a full-screen effect for the Built-in Render Pipeline and URP (Render Graph). Both versions share one shader and one pass driver.

## Install

In Package Manager, choose **Add package from disk** and select `com.crtroyale.shaders/package.json`. Copy that folder into your project's `Packages` folder if you want the project to be self-contained.

- **Built-in:** add **Rendering > CRT Royale** to the camera.
- **URP:** on your Universal Renderer asset, choose **Add Renderer Feature > CRT Royale Feature**. Its settings are on the feature, and it applies to Game cameras.

Requires Direct3D 11, Direct3D 12 or Vulkan. All 21 shader passes compile for these; only Direct3D 11 and 12 have been run.

## Settings

Every setting from the ReShade version is available, with the same defaults. Ones ReShade fixed at compile time are now ordinary settings: content box, pre-blur, antialiasing level and filter, beam table size, and bloom downsizing.

Scanline thickness and phosphor triad width are in **screen pixels**, as in ReShade, so they don't follow your art's pixel grid. For upscaled pixel art, set Scanline Thickness to the upscale factor (or a multiple) to line them up.

The effect expects a finished image, so it runs after URP's post-processing and clamps HDR values to the 0-1 range.

## Differences from ReShade

- Direct3D 9 code paths, `USE_VERTEX_UNCROPPING`, `RADEON_FIX` and the Intel compatibility mode are left out. They work around old drivers or give about 0.1 ms; only the slower, full-refresh uncrop is kept.
- OpenGL is not supported: the passes assume a top-left texture origin.
- Changing a setting or the resolution redraws the beam table and phosphor mask on the next frame, where ReShade does so while its overlay is open.

## Layout

| Path | Contents |
|---|---|
| `com.crtroyale.shaders` | The package |
| `com.crtroyale.testkit` | Test scene and capture harness |
| `TestBuiltIn`, `TestURP` | Test projects |
| `Tools/port_shaders.py` | Regenerates `Runtime/Shaders/Core/{lib,shaders}` from the ReShade source; edit this rather than the generated files |
| `Tools/compare_captures.py` | Compares captures |

## ReShade reference capture

1. Run the ReShade installer and select `Build/CrtTestPlayer/CrtTestPlayer.exe` with the **Direct3D 10/11/12** API. Choose the standard effects package, which supplies `ReShade.fxh`.
2. Copy the contents of `M:\CRT Royale\Shaders` into `Build/CrtTestPlayer/reshade-shaders/Shaders`.
3. Start `CrtTestPlayer.exe`, open the ReShade overlay, enable only `CRT_Royale` and leave its settings alone.
4. In ReShade's settings, set the screenshot format to PNG. Close the overlay, wait 5 seconds, then press Print Screen.
5. Save the screenshot as `Reference/reshade-default.png`.
6. Run the comparison:

```bash
.tmp/venv/Scripts/python -I Tools/compare_captures.py .tmp/captures "reshade=Reference/reshade-default.png"
```

## Licence

This port is distributed under the GNU GPL version 3 or later; the full text is in `LICENSE` at the repository root. The upstream crt-royale code is GPL-2.0-or-later (some files MIT), which permits this, and its copyright and licence notices are kept in the generated files. If you distribute a project containing this, GPL obligations may apply.

`Shaders/` holds the unmodified crt-royale-reshade 2.1.0 source that `Tools/port_shaders.py` reads.
