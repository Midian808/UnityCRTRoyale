"""Compares CRT Royale captures. Usage: python -I compare_captures.py <captures folder>"""
import pathlib
import sys

import numpy as np
from PIL import Image

root = pathlib.Path(sys.argv[1])


def load(path):
    return np.asarray(Image.open(path).convert("RGB"), dtype=np.int16)


def diff(a, b):
    if a.shape != b.shape:
        return f"size differs {a.shape} {b.shape}"
    d = np.abs(a - b)
    return f"max={d.max()} mean={d.mean():.4f} pixels>2={(d.max(axis=2) > 2).mean() * 100:.3f}%"


b, u = root / "builtin", root / "urp"
for name in sorted(p.name for p in b.glob("*.png")):
    print(f"builtin vs urp  {name}: {diff(load(b / name), load(u / name))}")
for pipe in (b, u):
    print(pipe.name, "default vs back-to-default:", diff(load(pipe / "01_default.png"), load(pipe / "05_back_to_default.png")))
    print(pipe.name, "grille first frame vs settled:", diff(load(pipe / "02_grille_first_frame.png"), load(pipe / "03_grille_settled.png")))
    print(pipe.name, "default vs grille (must differ):", diff(load(pipe / "01_default.png"), load(pipe / "03_grille_settled.png")))

gamma = root / "builtin-gamma"
if gamma.exists():
    print("linear vs gamma project:", diff(load(b / "01_default.png"), load(gamma / "01_default.png")))
    print("linear vs gamma input:", diff(load(b / "00_input.png"), load(gamma / "00_input.png")))

for extra in sys.argv[2:]:
    label, path = extra.split("=", 1)
    print(label, "vs builtin default:", diff(load(b / "01_default.png"), load(path)))

# Ramp brightness: mean output of each grey step against the input.
inp, out = load(b / "00_input.png"), load(b / "01_default.png")
print("ramp step input -> output mean:")
for step in range(0, 32, 5):
    x0, x1 = step * 60 + 10, step * 60 + 50
    print(f"  {step:2d}: {inp[380:500, x0:x1].mean():6.1f} -> {out[380:500, x0:x1].mean():6.1f}")

# Contact sheet of the variations.
names = ["06_curved_aa8", "07_gaussian_shadow", "08_interlaced_weave", "09_preblur_off", "10_cropped_4x3", "11_content_box"]
sheet = Image.new("RGB", (1920, 720))
for i, name in enumerate(names):
    tile = Image.open(b / f"{name}.png").convert("RGB").resize((640, 360), Image.LANCZOS)
    sheet.paste(tile, (640 * (i % 3), 360 * (i // 3)))
sheet.save(root / "contact_sheet.png")
for crop_name in ["01_default", "07_gaussian_shadow"]:
    Image.open(b / f"{crop_name}.png").convert("RGB").crop((80, 540, 400, 720)).resize((1280, 720), Image.NEAREST).save(root / f"zoom_{crop_name}.png")
