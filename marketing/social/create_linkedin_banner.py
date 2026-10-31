from pathlib import Path
import subprocess
import tempfile

from PIL import Image, ImageChops, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[2]
SOCIAL = ROOT / "marketing" / "social"
BACKGROUND = SOCIAL / "fedril-linkedin-banner-bg.png"
LOGO = ROOT / "apps" / "web" / "public" / "F.svg"
OUTPUT = SOCIAL / "fedril-linkedin-banner.png"

WIDTH = 1584
HEIGHT = 396
TEXT_X = 456


def cover_crop(image: Image.Image, width: int, height: int) -> Image.Image:
    scale = max(width / image.width, height / image.height)
    resized = image.resize(
        (round(image.width * scale), round(image.height * scale)),
        Image.Resampling.LANCZOS,
    )
    left = (resized.width - width) // 2
    top = (resized.height - height) // 2
    return resized.crop((left, top, left + width, top + height))


def render_clean_logo(size: int) -> Image.Image:
    """Rasterize the canonical SVG without retaining its white perimeter fringe."""
    with tempfile.TemporaryDirectory(prefix="fedril-linkedin-") as temporary_directory:
        rendered_logo = Path(temporary_directory) / "logo.png"
        subprocess.run(
            ["sips", "-s", "format", "png", str(LOGO), "--out", str(rendered_logo)],
            check=True,
            capture_output=True,
        )
        source = Image.open(rendered_logo).convert("RGBA").crop((48, 48, 452, 452))

    working_size = 512
    source = source.resize((working_size, working_size), Image.Resampling.LANCZOS)
    red, green, blue, alpha = source.split()
    lightness = ImageChops.darker(ImageChops.darker(red, green), blue)
    white_glyph = lightness.point(lambda value: 255 if value >= 235 else 0)
    white_glyph = ImageChops.multiply(white_glyph, alpha)

    # Exclude the narrow outer annulus that belongs to SVG edge antialiasing.
    interior = Image.new("L", (working_size, working_size), 0)
    ImageDraw.Draw(interior).ellipse((18, 18, working_size - 19, working_size - 19), fill=255)
    white_glyph = ImageChops.multiply(white_glyph, interior)

    clean_logo = Image.new("RGBA", (working_size, working_size), (0, 0, 0, 0))
    clean_mask = Image.new("L", (working_size, working_size), 0)
    ImageDraw.Draw(clean_mask).ellipse((1, 1, working_size - 2, working_size - 2), fill=255)
    clean_logo.paste("#659470", (0, 0, working_size, working_size), clean_mask)
    clean_logo.paste("#FFFFFF", (0, 0, working_size, working_size), white_glyph)
    return clean_logo.resize((size, size), Image.Resampling.LANCZOS)


def main() -> None:
    canvas = cover_crop(Image.open(BACKGROUND).convert("RGB"), WIDTH, HEIGHT).convert("RGBA")

    # Add a restrained readability veil without hiding the generated background.
    veil = Image.new("RGBA", canvas.size, (0, 0, 0, 0))
    veil_draw = ImageDraw.Draw(veil)
    for x in range(330, 1020):
        opacity = round(38 * (1 - (x - 330) / 690))
        veil_draw.line((x, 0, x, HEIGHT), fill=(5, 12, 11, opacity))
    canvas = Image.alpha_composite(canvas, veil)

    logo = render_clean_logo(76)
    canvas.alpha_composite(logo, (TEXT_X, 61))

    draw = ImageDraw.Draw(canvas)
    font_bold = "/System/Library/Fonts/Supplemental/Arial Bold.ttf"
    font_regular = "/System/Library/Fonts/Supplemental/Arial.ttf"

    headline_font = ImageFont.truetype(font_bold, 36)
    descriptor_font = ImageFont.truetype(font_regular, 25)
    url_font = ImageFont.truetype(font_bold, 22)
    posture_font = ImageFont.truetype(font_regular, 15)

    draw.text((TEXT_X + 101, 60), "Organize CMMC, DFARS and", font=headline_font, fill="#F7FAF7")
    draw.text((TEXT_X + 101, 103), "NIST SP 800-171 readiness", font=headline_font, fill="#F7FAF7")

    draw.rounded_rectangle((TEXT_X, 191, TEXT_X + 96, 195), radius=2, fill="#C7A45B")
    draw.text(
        (TEXT_X, 220),
        "FeDril | Compliance operations for small defense contractors",
        font=descriptor_font,
        fill="#F7FAF7",
    )
    draw.text((TEXT_X, 282), "fedril.com", font=url_font, fill="#F7FAF7")
    draw.rounded_rectangle((TEXT_X + 135, 280, TEXT_X + 326, 311), radius=15, fill="#375D4C")
    draw.text((TEXT_X + 151, 287), "NO-CUI WORKSPACE", font=posture_font, fill="#E6F0E8")

    canvas.convert("RGB").save(OUTPUT, format="PNG", optimize=True)


if __name__ == "__main__":
    main()
