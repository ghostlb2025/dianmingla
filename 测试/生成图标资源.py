from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "assets"
SOURCE = ASSETS / "点名啦图标.png"
ICO = ASSETS / "点名啦.ico"
AUDIT = ROOT / "测试" / "icon-size-audit.png"


def prepare_master(source: Path) -> Image.Image:
    image = Image.open(source).convert("RGBA")
    canvas = Image.new("RGBA", (1024, 1024), (0, 0, 0, 0))
    fitted = image.copy()
    fitted.thumbnail((1024, 1024), Image.Resampling.LANCZOS)
    canvas.alpha_composite(fitted, ((1024 - fitted.width) // 2, (1024 - fitted.height) // 2))

    # Image generation can leave a few isolated edge pixels.  Keep a generous
    # rounded safety envelope around the actual tile and remove only those
    # outliers so the taskbar icon has a clean transparent boundary.
    guard_large = Image.new("L", (4096, 4096), 0)
    guard_draw = ImageDraw.Draw(guard_large)
    guard_draw.rounded_rectangle((144, 144, 3952, 3952), radius=900, fill=255)
    guard = guard_large.resize((1024, 1024), Image.Resampling.LANCZOS)
    alpha = ImageChops.multiply(canvas.getchannel("A"), guard)
    canvas.putalpha(alpha)
    return canvas


def build_audit(master: Image.Image) -> None:
    sizes = [16, 24, 32, 48]
    sheet = Image.new("RGB", (900, 300), "white")
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.text((20, 14), "Actual-size and pixel-preview audit", fill=(31, 44, 63), font=font)

    for index, size in enumerate(sizes):
        x = 30 + index * 215
        icon = master.resize((size, size), Image.Resampling.LANCZOS)

        draw.rounded_rectangle((x, 48, x + 180, 132), 10, fill=(244, 247, 251))
        sheet.paste(icon, (x + (180 - size) // 2, 48 + (84 - size) // 2), icon)
        draw.text((x + 72, 137), f"{size}px", fill=(70, 82, 101), font=font)

        scale = max(2, 128 // size)
        enlarged = icon.resize((size * scale, size * scale), Image.Resampling.NEAREST)
        dark = Image.new("RGB", enlarged.size, (31, 35, 42))
        dark.paste(enlarged, (0, 0), enlarged)
        sheet.paste(dark, (x + (180 - dark.width) // 2, 162))

    sheet.save(AUDIT)


def main() -> None:
    master = prepare_master(SOURCE)
    master.save(SOURCE, optimize=True)
    master.save(ICO, format="ICO", sizes=[(16, 16), (24, 24), (32, 32), (48, 48), (256, 256)])
    build_audit(master)
    print(f"PNG={SOURCE}")
    print(f"ICO={ICO}")
    print(f"AUDIT={AUDIT}")


if __name__ == "__main__":
    main()
