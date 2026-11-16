"""
Builds the itch.io page theme kit for The Spice Route (all four chapters).

    python make_itch_page.py

Reads  : capture/stills/*.png and out/SpiceRoute_Trailer_1080p.mp4   (Chapter I cinematic capture)
         cap1..cap4/                                                  (autoplay -gifs captures of every chapter)
Writes : itch_page/           background, banner, logo, dividers, infographics, controls card, GIFs
         out/itch_screenshots new screenshots 16+ from Chapters II-IV and land play
"""
import glob
import os
import shutil
import subprocess

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
STILLS = os.path.join(HERE, "capture", "stills")
OUT = os.path.join(HERE, "itch_page")
SHOTS = os.path.join(HERE, "out", "itch_screenshots")
TRAILER = os.path.join(HERE, "out", "SpiceRoute_Trailer_1080p.mp4")
FONTS = r"C:\Windows\Fonts"

BG = (7, 11, 15)
BG2 = (14, 21, 27)
GOLD = (232, 185, 90)
GOLD_L = (242, 205, 125)
PARCH = (233, 220, 192)
MUTED = (150, 142, 125)
TEAL = (120, 225, 215)


def F(name, size):
    return ImageFont.truetype(os.path.join(FONTS, name), size)


def spaced(s, gap=1):
    out = []
    for i, ch in enumerate(s):
        out.append(ch)
        if i < len(s) - 1:
            out.append(" " * (gap * 2 if ch == " " else gap))
    return "".join(out)


def text(img, s, fnt, fill, center=None, xy=None, shadow=True, stroke=0):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    bb = d.textbbox((0, 0), s, font=fnt, stroke_width=stroke)
    w, h = bb[2] - bb[0], bb[3] - bb[1]
    if center:
        x, y = center[0] - w / 2 - bb[0], center[1] - h / 2 - bb[1]
    else:
        x, y = xy
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


def cap(chapter, pattern):
    """First file matching a pattern inside capN/ (autoplay capture of a chapter)."""
    hits = sorted(glob.glob(os.path.join(HERE, "cap%d" % chapter, pattern)))
    if not hits:
        raise FileNotFoundError("cap%d/%s" % (chapter, pattern))
    return hits[0]


def still(name):
    """A source picture: 'capN:pattern' from an autoplay capture, 'market', or a Chapter I cinematic still."""
    if name.startswith("cap"):
        n, pat = name[3:].split(":", 1)
        return Image.open(cap(int(n), pat)).convert("RGBA")
    if name == "market":
        return Image.open(os.path.join(HERE, "autoplay_tutorial", "02_market_open.png")).convert("RGBA").crop((500, 250, 2060, 1130))
    return Image.open(os.path.join(STILLS, name + ".png")).convert("RGBA")


# ------------------------------------------------------------------------- background
def background():
    W, H = 2560, 1600
    img = fit(still("07_sunset_echoes"), (W, H), (0.5, 0.35))
    img = img.filter(ImageFilter.GaussianBlur(3))
    img = Image.alpha_composite(img, Image.new("RGBA", (W, H), BG + (150,)))
    img = Image.alpha_composite(img, grad_alpha((W, H), lambda x, y: np.clip((y - 0.32) / 0.45, 0, 1) ** 1.2))
    img = Image.alpha_composite(img, grad_alpha((W, H), lambda x, y: (np.abs(x - 0.5) * 2) ** 3 * 0.6))
    img.convert("RGB").save(os.path.join(OUT, "background_2560x1600.jpg"), quality=90)


# ------------------------------------------------------------------------- logo + banner
def logo():
    W, H = 960, 300
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
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
    img.alpha_composite(lg.resize((640, 200), Image.LANCZOS), (8, 70))
    img = text(img, "The complete voyage  ·  four chapters  ·  four endings", F("georgiai.ttf", 21), PARCH, center=(328, 300))
    img.convert("RGB").save(os.path.join(OUT, "banner_960x360.jpg"), quality=93)


# ------------------------------------------------------------------------- section dividers
def dividers():
    names = {
        "the_voyage": "THE VOYAGE",
        "four_chapters": "FOUR CHAPTERS",
        "seven_shifts": "THE SEVEN SHIFTS",
        "land_crew_ship": "LAND, CREW AND SHIP",
        "how_to_play": "HOW TO PLAY",
        "features": "FEATURES",
        "made_in_code": "MADE ENTIRELY IN CODE",
        "credits": "CREDITS",
    }
    for key, label in names.items():
        W, H = 960, 110
        img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        img = text(img, spaced(label), F("georgiab.ttf", 34), GOLD_L, center=(W / 2, 46))
        d = ImageDraw.Draw(img)
        ornament(d, W // 2, 90, 300, GOLD + (230,))
        img.save(os.path.join(OUT, "divider_%s.png" % key))


def tile_grid(tiles, name, per_row, TW, TH, title_size=20, sub_size=15, CAP=62, PAD=24):
    """A grid of captioned pictures on the page colour; the last row is centred."""
    W = 960
    rows = (len(tiles) + per_row - 1) // per_row
    H = rows * (TH + CAP) + (rows + 1) * PAD
    img = Image.new("RGBA", (W, H), BG2 + (255,))
    for i, (title, sub, src, focus, col) in enumerate(tiles):
        r = i // per_row
        in_row = min(per_row, len(tiles) - r * per_row)
        row_w = in_row * TW + (in_row - 1) * PAD
        x = (W - row_w) // 2 + (i % per_row) * (TW + PAD)
        y = PAD + r * (TH + CAP + PAD)
        img.alpha_composite(fit(still(src), (TW, TH), focus), (x, y))
        d = ImageDraw.Draw(img)
        d.rectangle([x - 1, y - 1, x + TW, y + TH], outline=col + (255,), width=2)
        img = text(img, title, F("georgiab.ttf", title_size), col, center=(x + TW / 2, y + TH + 20), shadow=False)
        img = text(img, sub, F("georgiai.ttf", sub_size), PARCH, center=(x + TW / 2, y + TH + 44), shadow=False)
    img.convert("RGB").save(os.path.join(OUT, name))


# ------------------------------------------------------------------------- seven shifts infographic
def shifts():
    c = (0.5, 0.5)
    tiles = [
        ("WIND SHIFT", "The monsoon turns mid-voyage", "04_open_sea_day", c, GOLD),
        ("COURSE SHIFT", "Run for shelter behind Kadal", "03_sail_dawn", c, GOLD),
        ("TIDE SHIFT", "Cross sandbars at high water", SRC["tide"], c, GOLD),
        ("WEATHER SHIFT", "Storm, fog and night", "05_storm", c, GOLD),
        ("PERSPECTIVE SHIFT", "Take the helm, or walk ashore", "hero_dawn_close", c, GOLD),
        ("TRADE SHIFT", "Buy cheap, sell far away", "market", c, GOLD),
        ("ERA SHIFT", "See the sea as it once was", "08_era_shift", c, TEAL),
    ]
    tile_grid(tiles, "seven_shifts_960.png", 4, 212, 119, 16, 13, 58, 20)


# ------------------------------------------------------------------------- four chapters
def chapters():
    c = (0.5, 0.5)
    tiles = [
        ("I  ·  THE MALABAR EXPEDITION", "Muziris, a monsoon squall, the Isle of Echoes", "01_muziris_dawn", c, GOLD),
        ("II  ·  THE GATE OF TEARS", "Fog reefs, Aden, a night strait, the Black Compass", SRC["ch2"], c, GOLD),
        ("III  ·  THE GREAT MARKETPLACE", "The old kings' canal, tides, Alexandria by night", SRC["ch3"], c, GOLD),
        ("IV  ·  ACROSS SEVEN SEAS", "A Guild fleet, the Storm Wall, four endings", SRC["ch4"], c, TEAL),
    ]
    tile_grid(tiles, "four_chapters_960.png", 2, 440, 248, 19, 15, 64, 26)


# ------------------------------------------------------------------------- land, crew and ship
def ledger():
    """Ashore + the three Captain's Ledger pages."""
    c = (0.5, 0.5)
    tiles = [
        ("GO ASHORE", "Walk the ports and islands on foot", SRC["ashore"], c, GOLD),
        ("SKILL TREES", "Navigator, Merchant, Warrior", SRC["skills"], c, TEAL),
        ("CREW AND LOYALTY", "Hire, assign stations, pay wages", SRC["crew"], c, GOLD),
        ("UPGRADES AND SHIP CLASSES", "Five upgrades, five kinds of ship", SRC["ship"], c, GOLD),
    ]
    tile_grid(tiles, "land_crew_ship_960.png", 2, 440, 248, 19, 15, 64, 26)


# ------------------------------------------------------------------------- controls card
def keycap(img, x, y, label):
    f = F("seguisb.ttf", 21)
    d = ImageDraw.Draw(img)
    bb = d.textbbox((0, 0), label, font=f)
    w = max(52, bb[2] - bb[0] + 28)
    d.rounded_rectangle([x, y + 4, x + w, y + 48], 8, fill=(4, 6, 8, 255))
    d.rounded_rectangle([x, y, x + w, y + 44], 8, fill=(30, 36, 42, 255), outline=GOLD + (255,), width=2)
    d.text((x + w / 2 - (bb[2] - bb[0]) / 2 - bb[0], y + 22 - (bb[3] - bb[1]) / 2 - bb[1]), label, font=f, fill=GOLD_L + (255,))
    return x + w


def controls():
    rows = [
        ("AT SEA", None),
        (["W", "S"], "Raise / lower the sail  (3 settings)"),
        (["A", "D"], "Steer: hold to turn, let go to sail straight"),
        (["T"], "Auto-steer to the gold diamond (avoids land)"),
        (["SHIFT"], "ERA SHIFT: see the sea as it once was"),
        (["E"], "Trade, dock, interact"),
        (["LMB"], "Loose fire-arrows (enemy beside you)"),
        (["RMB"], "Hold to aim the volley"),
        (["L"], "Douse or light your lantern (stealth)"),
        (["Q"], "Helm view (Perspective Shift)"),
        ("ON LAND", None),
        (["F"], "Go ashore / return to the ship"),
        (["W", "A", "S", "D"], "Walk"),
        (["SPACE"], "Jump"),
        (["E"], "Use: shops, chests, inscriptions"),
        ("ANYWHERE", None),
        (["TAB"], "Captain's Ledger: skills, crew, ship"),
        (["M"], "Your father's chart"),
        (["P"], "Pause   ·   Enter skips dialogue"),
    ]
    W = 960
    H = 40 + sum(44 if r[1] is None else 50 for r in rows) + 20
    img = Image.new("RGBA", (W, H), BG2 + (255,))
    d = ImageDraw.Draw(img)
    d.rounded_rectangle([6, 6, W - 7, H - 7], 16, outline=GOLD + (140,), width=2)
    y = 30
    for keys, label in rows:
        if label is None:
            img = text(img, spaced(keys), F("georgiab.ttf", 20), TEAL, xy=(60, y + 10), shadow=False)
            y += 44
            continue
        x = 60
        for k in keys:
            x = keycap(img, x, y, k) + 8
        img = text(img, label, F("georgia.ttf", 22), PARCH, xy=(330, y + 9), shadow=False)
        y += 50
    img.convert("RGB").save(os.path.join(OUT, "controls_960.png"))


# ------------------------------------------------------------------------- gifs
def make_gif(inputs, vf, out):
    pal = os.path.join(OUT, "_pal.png")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error"] + inputs + ["-vf", vf + ",palettegen=max_colors=128:stats_mode=diff", pal], check=True)
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error"] + inputs + ["-i", pal, "-lavfi", vf + " [x]; [x][1:v] paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle", out], check=True)
    os.remove(pal)
    print(os.path.basename(out), "%.1f MB" % (os.path.getsize(out) / 1e6))


def gifs():
    # Chapter I: cut from the trailer master (1920x1080 with 70px letterbox bars)
    for name, ss, t in [("gif_sailing", 11.6, 3.2), ("gif_storm", 23.6, 3.2), ("gif_battle", 31.2, 3.2), ("gif_era_shift", 43.6, 3.6), ("gif_sunset", 38.2, 3.0)]:
        if os.path.exists(os.path.join(OUT, name + ".gif")):
            continue   # already cut; delete the file to re-cut it from the trailer
        make_gif(["-ss", str(ss), "-t", str(t), "-i", TRAILER], "crop=1920:940:0:70,fps=14,scale=630:-1:flags=lanczos", os.path.join(OUT, name + ".gif"))
    # Chapters II-IV and land: frame bursts recorded by the autoplay bot (20 fps)
    for name, (chapter, burst) in GIFS.items():
        d = os.path.join(HERE, "cap%d" % chapter, "gif_" + burst)
        if not os.path.isdir(d):
            print("missing burst", d)
            continue
        # renumber the frames so a dropped capture frame does not break the sequence
        tmp = os.path.join(OUT, "_frames")
        shutil.rmtree(tmp, ignore_errors=True)
        os.makedirs(tmp)
        for i, f in enumerate(sorted(glob.glob(os.path.join(d, "*.png")))):
            shutil.copy(f, os.path.join(tmp, "%03d.png" % i))
        make_gif(["-framerate", "20", "-i", os.path.join(tmp, "%03d.png")], "fps=11,scale=600:-1:flags=lanczos", os.path.join(OUT, name + ".gif"))
        shutil.rmtree(tmp, ignore_errors=True)


# ------------------------------------------------------------------------- screenshots
def screenshots():
    for i, (label, src) in enumerate(NEW_SHOTS):
        img = fit(still(src), (1920, 1080)).convert("RGB")
        img.save(os.path.join(SHOTS, "%02d_%s.jpg" % (16 + i, label)), quality=90)
    print(len(NEW_SHOTS), "new screenshots")


# Which captured picture feeds which panel. Patterns are globbed inside capN/.
SRC = {
    "tide": "cap3:*step2.png",
    "ch2": "cap2:still_flagship.png",
    "ch3": "cap3:still_canal.png",
    "ch4": "cap4:still_last_map.png",
    "ashore": "cap1:*land_ashore.png",
    "skills": "cap1:*ledger_skills.png",
    "crew": "cap1:*ledger_crew_hired.png",
    "ship": "cap1:*ledger_ship_bought.png",
}
GIFS = {
    "gif_ashore": (1, "ashore"),
    "gif_fog_beacons": (2, "fog_beacons"),
    "gif_stealth": (2, "stealth"),
    "gif_flagship": (2, "flagship"),
    "gif_canal_tide": (3, "canal"),
    "gif_pharos_night": (3, "pharos_night"),
    "gif_archive": (4, "archive"),
    "gif_fleet": (4, "fleet"),
    "gif_storm_wall": (4, "storm_wall"),
    "gif_last_map": (4, "last_map"),
}
NEW_SHOTS = [
    ("ashore_muziris", "cap1:*land_ashore.png"),
    ("ledger_skills", "cap1:*ledger_skills.png"),
    ("ledger_crew", "cap1:*ledger_crew_hired.png"),
    ("ledger_ship", "cap1:*ledger_ship_bought.png"),
    ("ch2_fog_beacons", "cap2:still_fog_beacons.png"),
    ("ch2_gate_of_tears", "cap2:still_stealth.png"),
    ("ch2_black_compass", "cap2:still_flagship.png"),
    ("ch3_canal_tide", "cap3:still_canal.png"),
    ("ch3_alexandria_night", "cap3:still_pharos_night.png"),
    ("ch3_escape", "cap3:still_escape.png"),
    ("ch4_guild_fleet", "cap4:still_fleet.png"),
    ("ch4_storm_wall", "cap4:still_storm_wall.png"),
    ("ch4_sunken_archive", "cap4:still_archive.png"),
    ("ch4_last_map", "cap4:still_last_map.png"),
    ("ch4_the_choice", "cap4:*choice.png"),
    ("ch4_ending", "cap4:*ending_screen.png"),
]


def main():
    os.makedirs(OUT, exist_ok=True)
    background()
    lg = logo()
    banner(lg)
    dividers()
    shifts()
    chapters()
    ledger()
    controls()
    screenshots()
    gifs()
    print("done ->", OUT)


if __name__ == "__main__":
    main()
