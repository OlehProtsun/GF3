"""Generate checked-in PWA assets offline. Pillow is needed only by developers running this script."""

from pathlib import Path

from PIL import Image


FRONTEND = Path(__file__).resolve().parents[1]
BACKGROUND = "#f3f4f6"
# Portrait CSS viewport width, height and device pixel ratio.
VIEWPORTS = [
    (320, 568, 2), (375, 667, 2), (375, 812, 3), (390, 844, 3),
    (393, 852, 3), (402, 874, 3), (414, 896, 2), (414, 896, 3),
    (428, 926, 3), (430, 932, 3), (440, 956, 3),
]


def save_centered(source, size, icon_size, path):
    canvas = Image.new("RGB", size, BACKGROUND)
    icon = source.copy()
    icon.thumbnail((icon_size, icon_size), Image.Resampling.LANCZOS)
    canvas.paste(icon, ((canvas.width - icon.width) // 2, (canvas.height - icon.height) // 2), icon)
    path.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(path, optimize=True)


def main():
    with Image.open(FRONTEND / "branding/gf-app-icon-source.png") as original:
        source = original.convert("RGBA")
    for width, height, dpr in VIEWPORTS:
        pixels = width * dpr, height * dpr
        save_centered(source, pixels, 128 * dpr, FRONTEND / f"public/splash/ios-{pixels[0]}x{pixels[1]}.png")

    # The 280px square fits inside the maskable icon's 40%-radius safe circle.
    save_centered(source, (512, 512), 280, FRONTEND / "public/icons/gf-icon-maskable-512.png")


if __name__ == "__main__":
    main()
