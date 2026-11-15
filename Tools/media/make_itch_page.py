"""
Builds an itch.io page theme kit for The Spice Route.

    python make_itch_page.py

Reads  : capture/stills/*.png, out/SpiceRoute_Trailer_1080p.mp4, out/itch_screenshots/
Writes : itch_page/  (background, banner, logo, dividers, infographic, controls card, GIFs)
"""
import os
import subprocess

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
STILLS = os.path.join(HERE, "capture", "stills")
OUT = os.path.join(HERE, "itch_page")
TRAILER = os.path.join(HERE, "out", "SpiceRoute_Trailer_1080p.mp4")
FONTS = r"C:\Windows\Fonts"

BG = (7, 11, 15)
BG2 = (14, 21, 27)
GOLD = (232, 185, 90)
GOLD_L = (242, 205, 125)
PARCH = (233, 220, 192)
MUTED = (150, 142, 125)
TEAL = (120, 225, 215)
INK = (40, 26, 12)


def F(name, size):
    return ImageFont.truetype(os.path.join(FONTS, name), size)


def spaced(s, gap=1):
    out = []
    for i, ch in enumerate(s):
        out.append(ch)
        if i < len(s) - 1:
            out.append(" " * (gap * 2 if ch == " " else gap))
    return "".join(out)


def text(img, s, fnt, fill, center=None, xy=None, shadow=True, stroke=0, anchor_right=False):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    bb = d.textbbox((0, 0), s, font=fnt, stroke_width=stroke)
    w, h = bb[2] - bb[0], bb[3] - bb[1]
    if center:
        x, y = center[0] - w / 2 - bb[0], center[1] - h / 2 - bb[1]
    else:
        x, y = xy
        if anchor_right:
            x -= w
    if shadow:
        sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(sh).text((x + 2, y + 3), s, font=fnt, fill=(0, 0, 0, 220), stroke_width=stroke)
        layer = Image.alpha_composite(layer, sh.filter(ImageFilter.GaussianBlur(5)))
        d = ImageDraw.Draw(layer)
    d.text((x, y), s, font=fnt, fill=fill + (255,), stroke_width=stroke, stroke_fill=(25, 15, 6, 255))
    return Image.alpha_composite(img, layer)


def fit(img, size, focus=(0.5, 0.5)):
    W, H = size
    r = max(W / img.width, H / img.height)
    img = img.resize((int(img.width * r + 0.5), int(img.height * r + 0.5)), Image.LANCZOS)
    x = int((img.width - W) * focus[0])
    y = int((img.height - H) * focus[1])
    return img.crop((x, y, x + W, y + H))


def grad_alpha(size, fn):
    w, h = size
    y, x = np.mgrid[0:h, 0:w]
    a = np.clip(fn(x / max(1, w - 1), y / max(1, h - 1)), 0, 1)
    m = np.zeros((h, w, 4), np.uint8)
    m[..., :3] = BG
    m[..., 3] = (a * 255).astype(np.uint8)
    return Image.fromarray(m, "RGBA")


def ornament(d, cx, cy, half, color, width=2):
    d.line([(cx - half, cy), (cx - 14, cy)], fill=color, width=width)
    d.line([(cx + 14, cy), (cx + half, cy)], fill=color, width=width)
    d.polygon([(cx, cy - 8), (cx + 8, cy), (cx, cy + 8), (cx - 8, cy)], fill=color)


def still(name):
    if name == "market":
        return Image.open(os.path.join(HERE, "autoplay_tutorial", "02_market_open.png")).convert("RGBA").crop((500, 250, 2060, 1130))
    return Image.open(os.path.join(STILLS, name + ".png")).convert("RGBA")


# ------------------------------------------------------------------------- background
def background():
    W, H = 2560, 1600
    img = fit(still("07_sunset_echoes"), (W, H), (0.5, 0.35))
    img = img.filter(ImageFilter.GaussianBlur(3))
    dark = Image.new("RGBA", (W, H), BG + (150,))
    img = Image.alpha_composite(img, dark)
    # keep the sky visible at the top, fade to page colour lower down and at the centre column edges
    img = Image.alpha_composite(img, grad_alpha((W, H), lambda x, y: np.clip((y - 0.32) / 0.45, 0, 1) ** 1.2))
    img = Image.alpha_composite(img, grad_alpha((W, H), lambda x, y: (np.abs(x - 0.5) * 2) ** 3 * 0.6))
    img.convert("RGB").save(os.path.join(OUT, "background_2560x1600.jpg"), quality=90)
    # a lighter, seamless parchment-texture tile for an alternative look
    tile = Image.open(os.path.join(HERE, "itch_page", "_parch_src.png")) if os.path.exists(os.path.join(OUT, "_parch_src.png")) else None
    return img


# ------------------------------------------------------------------------- logo + banner
def logo():
    W, H = 960, 300
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    img = text(img, spaced("A VOYAGE FROM THE MALABAR COAST"), F("seguisb.ttf", 18), MUTED, center=(W / 2, 40))
    img = text(img, "THE SPICE ROUTE", F("georgiab.ttf", 92), GOLD_L, center=(W / 2, 128), stroke=2)
    d = ImageDraw.Draw(img)
    ornament(d, W // 2, 200, 330, GOLD + (255,))
    img = text(img, spaced("ACROSS SEVEN SEAS"), F("seguisb.ttf", 28), PARCH, center=(W / 2, 244))
    img.save(os.path.join(OUT, "logo_transparent_960x300.png"))
    return img


def banner(lg):
    W, H = 960, 360
    img = fit(still("hero_sunset_close"), (W, H), (0.75, 0.42))
    img = Image.alpha_composite(img, grad_alpha((W, H), lambda x, y: np.clip(1.15 - x * 1.6, 0, 0.92)))
    img = Image.alpha_composite(img, grad_alpha((W, H), lambda x, y: np.clip((y - 0.75) / 0.25, 0, 1)))
    small = lg.resize((640, 200), Image.LANCZOS)
    img.alpha_composite(small, (8, 70))
    img = text(img, "Chapter I  —  The Malabar Expedition", F("georgiai.ttf", 22), PARCH, center=(328, 300))
    img.convert("RGB").save(os.path.join(OUT, "banner_960x360.jpg"), quality=93)


# ------------------------------------------------------------------------- section dividers
def dividers():
    names = {
        "the_voyage": "THE VOYAGE",
        "seven_shifts": "THE SEVEN SHIFTS",
        "how_to_play": "HOW TO PLAY",
        "features": "FEATURES",
        "made_in_code": "MADE ENTIRELY IN CODE",
        "credits": "CREDITS",
    }
    for key, label in names.items():
        W, H = 960, 110
        img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        d = ImageDraw.Draw(img)
        img = text(img, spaced(label), F("georgiab.ttf", 34), GOLD_L, center=(W / 2, 46))
        d = ImageDraw.Draw(img)
        ornament(d, W // 2, 90, 300, GOLD + (230,))
        img.save(os.path.join(OUT, "divider_%s.png" % key))


# ------------------------------------------------------------------------- seven shifts infographic
def shifts():
    tiles = [
        ("WIND SHIFT", "The monsoon turns mid-voyage", "04_open_sea_day"),
        ("WEATHER SHIFT", "Survive a monsoon squall", "05_storm"),
        ("COURSE SHIFT", "Run for shelter behind Kadal", "03_sail_dawn"),
        ("PERSPECTIVE SHIFT", "Stand at the helm with Q", "hero_dawn_close"),
        ("TRADE SHIFT", "Buy cheap, sell after the storm", "market"),
        ("ERA SHIFT", "See the sea as it once was", "08_era_shift"),
    ]
    TW, TH, PAD, CAP = 300, 169, 30, 64
    W = 3 * TW + 4 * PAD
    H = 2 * (TH + CAP) + 3 * PAD - 10
    img = Image.new("RGBA", (W, H), BG2 + (255,))
    d = ImageDraw.Draw(img)
    for i, (title, sub, src) in enumerate(tiles):
        cx, cy = i % 3, i // 3
        x = PAD + cx * (TW + PAD)
        y = PAD + cy * (TH + CAP + PAD - 10)
        t = fit(still(src), (TW, TH))
        if title == "ERA SHIFT":
            pass
        img.alpha_composite(t, (x, y))
        d = ImageDraw.Draw(img)
        col = TEAL if title == "ERA SHIFT" else GOLD
        d.rectangle([x - 1, y - 1, x + TW, y + TH], outline=col + (255,), width=2)
        img = text(img, title, F("georgiab.ttf", 22), col, center=(x + TW / 2, y + TH + 22), shadow=False)
        img = text(img, sub, F("georgiai.ttf", 16), PARCH, center=(x + TW / 2, y + TH + 46), shadow=False)
    img.convert("RGB").save(os.path.join(OUT, "seven_shifts_960.png"))


# ------------------------------------------------------------------------- controls card
def keycap(img, x, y, label, w=None):
    f = F("seguisb.ttf", 22)
    d = ImageDraw.Draw(img)
    bb = d.textbbox((0, 0), label, font=f)
    w = w or max(52, bb[2] - bb[0] + 30)
    d.rounded_rectangle([x, y + 4, x + w, y + 52], 8, fill=(4, 6, 8, 255))
    d.rounded_rectangle([x, y, x + w, y + 48], 8, fill=(30, 36, 42, 255), outline=GOLD + (255,), width=2)
    d.text((x + w / 2 - (bb[2] - bb[0]) / 2 - bb[0], y + 24 - (bb[3] - bb[1]) / 2 - bb[1]), label, font=f, fill=GOLD_L + (255,))
    return x + w


def controls():
    W, H = 960, 560
    img = Image.new("RGBA", (W, H), BG2 + (255,))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([6, 6, W - 7, H - 7], 16, outline=GOLD + (140,), width=2)
    rows = [
        (["W", "S"], "Raise / lower the sail  (3 settings)"),
        (["A", "D"], "Steer — hold to turn, let go to sail straight"),
        (["T"], "Auto-steer to the gold diamond (avoids land)"),
        (["SHIFT"], "ERA SHIFT — see the sea as it once was"),
        (["E"], "Trade, dock, open the package"),
        (["LMB"], "Loose fire-arrows (enemy beside you)"),
        (["RMB"], "Hold to aim the volley"),
        (["Q"], "Helm view (Perspective Shift)"),
        (["M"], "Your father's chart"),
        (["P"], "Pause   ·   Enter skips dialogue"),
    ]
    y = 36
    for keys, label in rows:
        x = 60
        for k in keys:
            x = keycap(img, x, y, k) + 10
        img = text(img, label, F("georgia.ttf", 23), PARCH, xy=(260, y + 10), shadow=False)
        y += 50
    img.convert("RGB").save(os.path.join(OUT, "controls_960.png"))


# ------------------------------------------------------------------------- gifs
def gifs():
    # trailer master: 1920x1080 with 70px letterbox bars; (name, start s, duration s)
    clips = [("gif_sailing", 11.6, 3.2), ("gif_storm", 23.6, 3.2), ("gif_battle", 31.2, 3.2), ("gif_era_shift", 43.6, 3.6), ("gif_sunset", 38.2, 3.0)]
    for name, ss, t in clips:
        out = os.path.join(OUT, name + ".gif")
        vf = "crop=1920:940:0:70,fps=14,scale=630:-1:flags=lanczos"
        pal = os.path.join(OUT, "_pal.png")
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-ss", str(ss), "-t", str(t), "-i", TRAILER, "-vf", vf + ",palettegen=max_colors=160:stats_mode=diff", pal], check=True)
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-ss", str(ss), "-t", str(t), "-i", TRAILER, "-i", pal, "-lavfi",
                        vf + " [x]; [x][1:v] paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle", out], check=True)
        os.remove(pal)
        print(name, "%.1f MB" % (os.path.getsize(out) / 1e6))


def main():
    os.makedirs(OUT, exist_ok=True)
    background()
    lg = logo()
    banner(lg)
    dividers()
    shifts()
    controls()
    gifs()
    print("done ->", OUT)


if __name__ == "__main__":
    main()
