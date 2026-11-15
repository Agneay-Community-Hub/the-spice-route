"""
Builds marketing media for The Spice Route from the Unity capture output.

    python make_media.py <capture_dir> <autoplay_dir> <out_dir>

capture_dir : output of SpiceRoute.exe -capture -out <dir>   (frames/, stills/, audio/, timeline.csv)
autoplay_dir: output of SpiceRoute.exe -autoplay -shots <dir> (in-game screenshots with HUD)
out_dir     : where trailer.mp4, thumbnails, covers and screenshots are written
"""
import csv
import os
import subprocess
import sys
import wave

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

FPS = 30
FONTS = r"C:\Windows\Fonts"
GOLD = (242, 200, 115)
PARCH = (246, 236, 214)
TEAL = (120, 235, 225)


def font(name, size):
    return ImageFont.truetype(os.path.join(FONTS, name), size)


def spaced(s, gap=1):
    out = []
    for i, ch in enumerate(s):
        out.append(ch)
        if i < len(s) - 1:
            out.append(" " * (gap * 2 if ch == " " else gap))
    return "".join(out)


def draw_text(img, text, fnt, fill, center_xy=None, xy=None, alpha=1.0, shadow=True, stroke=0):
    layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(layer)
    bbox = d.textbbox((0, 0), text, font=fnt, stroke_width=stroke)
    w, h = bbox[2] - bbox[0], bbox[3] - bbox[1]
    if center_xy is not None:
        x, y = center_xy[0] - w / 2 - bbox[0], center_xy[1] - h / 2 - bbox[1]
    else:
        x, y = xy
    a = int(255 * max(0.0, min(1.0, alpha)))
    if shadow:
        sh = Image.new("RGBA", img.size, (0, 0, 0, 0))
        ImageDraw.Draw(sh).text((x + 3, y + 4), text, font=fnt, fill=(0, 0, 0, int(a * 0.85)), stroke_width=stroke)
        sh = sh.filter(ImageFilter.GaussianBlur(6))
        layer = Image.alpha_composite(sh, layer)
        d = ImageDraw.Draw(layer)
    d.text((x, y), text, font=fnt, fill=fill + (a,), stroke_width=stroke, stroke_fill=(20, 12, 5, a))
    return Image.alpha_composite(img, layer)


def vignette(size, strength=0.55):
    w, h = size
    y, x = np.ogrid[0:h, 0:w]
    r = np.sqrt(((x - w / 2) / (w / 2)) ** 2 + ((y - h / 2) / (h / 2)) ** 2)
    a = np.clip((r - 0.55) * strength * 1.6, 0, 1) * 255
    m = np.zeros((h, w, 4), np.uint8)
    m[..., 3] = a.astype(np.uint8)
    return Image.fromarray(m, "RGBA")


def left_shade(size, width_frac=0.62, strength=0.85):
    w, h = size
    x = np.linspace(0, 1, w)
    a = np.clip(1 - x / width_frac, 0, 1) ** 1.4 * 255 * strength
    m = np.zeros((h, w, 4), np.uint8)
    m[..., 3] = np.tile(a.astype(np.uint8), (h, 1))
    return Image.fromarray(m, "RGBA")


def bottom_shade(size, frac=0.45, strength=0.8):
    w, h = size
    y = np.linspace(0, 1, h)
    a = np.clip((y - (1 - frac)) / frac, 0, 1) ** 1.3 * 255 * strength
    m = np.zeros((h, w, 4), np.uint8)
    m[..., 3] = np.tile(a.astype(np.uint8)[:, None], (1, w))
    return Image.fromarray(m, "RGBA")


# --------------------------------------------------------------------------- trailer
CARDS = {
    "01_muziris_dawn": [("small", "GITHUB GAME OFF 2026   ·   THEME: SHIFT", 0.05, 0.55), ("place", "MUZIRIS  ·  THE MALABAR COAST", 0.4, 0.98)],
    "02_temple": [("line", "An ancient port of pepper, temples and monsoon winds", 0.1, 0.95)],
    "03_sail_dawn": [("line", "Your father vanished chasing a route no chart recorded.", 0.1, 0.95)],
    "04_open_sea_day": [("big", "WIND SHIFT", 0.1, 0.92), ("sub", "Read the monsoon. Trim your sail.", 0.2, 0.92)],
    "05_storm": [("big", "WEATHER SHIFT", 0.08, 0.92), ("sub", "Survive the squall.", 0.2, 0.92)],
    "06_battle": [("big", "BLACK SAILS", 0.08, 0.9), ("sub", "Fire-arrows  ·  broadsides  ·  ramming", 0.2, 0.9)],
    "07_sunset_echoes": [("place", "THE ISLE OF ECHOES", 0.15, 0.95)],
    "08_era_shift": [("big", "ERA SHIFT", 0.3, 0.95), ("sub", "See the sea as it once was.", 0.4, 0.95)],
    "09_vellimeen": [("line", "Follow the ghost of the Vellimeen through the Shoals of Thorns.", 0.1, 0.95)],
    "10_ras_al_mira": [("big", "TRADE SHIFT", 0.1, 0.92), ("sub", "Sell your spices at the forgotten outpost.", 0.2, 0.92)],
    "11_finale": [("title", "THE SPICE ROUTE", 0.15, 1.0), ("title2", "ACROSS SEVEN SEAS", 0.25, 1.0),
                  ("tag", "Chapter I  —  The Malabar Expedition", 0.35, 1.0), ("cta", "PLAY FREE IN YOUR BROWSER  ·  itch.io", 0.5, 1.0)],
}

MUSIC = {
    "01_muziris_dawn": "music_calm", "02_temple": "music_calm", "03_sail_dawn": "music_calm", "04_open_sea_day": "music_calm",
    "05_storm": "music_storm", "06_battle": "music_combat", "07_sunset_echoes": "music_mystery", "08_era_shift": "music_mystery",
    "09_vellimeen": "music_mystery", "10_ras_al_mira": "music_calm", "11_finale": "music_calm",
}


def overlay_card(img, kind, text, alpha):
    W, H = img.size
    if kind == "small":
        return draw_text(img, spaced(text), font("seguisb.ttf", 26), PARCH, center_xy=(W / 2, H * 0.12), alpha=alpha)
    if kind == "place":
        return draw_text(img, spaced(text), font("georgiab.ttf", 46), GOLD, center_xy=(W / 2, H * 0.8), alpha=alpha)
    if kind == "line":
        return draw_text(img, text, font("georgiai.ttf" if os.path.exists(os.path.join(FONTS, "georgiai.ttf")) else "georgia.ttf", 40), PARCH, center_xy=(W / 2, H * 0.84), alpha=alpha)
    if kind == "big":
        return draw_text(img, spaced(text), font("georgiab.ttf", 96), GOLD, center_xy=(W / 2, H * 0.42), alpha=alpha, stroke=2)
    if kind == "sub":
        return draw_text(img, text, font("georgia.ttf", 40), PARCH, center_xy=(W / 2, H * 0.53), alpha=alpha)
    if kind == "title":
        return draw_text(img, spaced(text), font("georgiab.ttf", 108), GOLD, center_xy=(W / 2, H * 0.36), alpha=alpha, stroke=2)
    if kind == "title2":
        return draw_text(img, spaced(text), font("seguisb.ttf", 40), PARCH, center_xy=(W / 2, H * 0.47), alpha=alpha)
    if kind == "tag":
        return draw_text(img, text, font("georgia.ttf", 34), PARCH, center_xy=(W / 2, H * 0.55), alpha=alpha)
    if kind == "cta":
        return draw_text(img, spaced(text), font("seguisb.ttf", 28), TEAL, center_xy=(W / 2, H * 0.86), alpha=alpha)
    return img


def build_trailer(cap, out):
    timeline = []
    with open(os.path.join(cap, "timeline.csv")) as f:
        for row in csv.reader(f):
            timeline.append((row[0], int(row[1]), int(row[2])))
    tmp = os.path.join(out, "_trailer_frames")
    os.makedirs(tmp, exist_ok=True)
    vig = vignette((1920, 1080), 0.5)
    lb = Image.new("RGBA", (1920, 1080), (0, 0, 0, 0))
    ImageDraw.Draw(lb).rectangle([0, 0, 1920, 70], fill=(0, 0, 0, 255))
    ImageDraw.Draw(lb).rectangle([0, 1010, 1920, 1080], fill=(0, 0, 0, 255))
    k = 0
    for si, (name, start, n) in enumerate(timeline):
        fade_in = 30 if si == 0 else 9
        fade_out = 45 if si == len(timeline) - 1 else 9
        for i in range(n):
            src = os.path.join(cap, "frames", "%05d.jpg" % (start + i))
            img = Image.open(src).convert("RGBA")
            img = Image.alpha_composite(img, vig)
            u = i / max(1, n - 1)
            if name == "11_finale":
                shade = Image.new("RGBA", img.size, (0, 0, 0, int(110 * min(1, u * 3))))
                img = Image.alpha_composite(img, shade)
            for kind, text, a0, a1 in CARDS.get(name, []):
                if a0 <= u <= a1:
                    alpha = min(1, (u - a0) / 0.08, (a1 - u) / 0.08 if a1 < 1 else 1)
                    img = overlay_card(img, kind, text, alpha)
            img = Image.alpha_composite(img, lb)
            fade = min(1.0, i / fade_in, (n - 1 - i) / fade_out)
            if fade < 1:
                img = Image.blend(Image.new("RGBA", img.size, (0, 0, 0, 255)), img, max(0.0, fade))
            img.convert("RGB").save(os.path.join(tmp, "%05d.jpg" % k), quality=92)
            k += 1
        print("composited", name)
    total = k / FPS
    audio = build_audio(cap, timeline, total, out)
    mp4 = os.path.join(out, "SpiceRoute_Trailer_1080p.mp4")
    subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-framerate", str(FPS), "-i", os.path.join(tmp, "%05d.jpg"), "-i", audio,
                    "-c:v", "libx264", "-preset", "slow", "-crf", "18", "-pix_fmt", "yuv420p", "-c:a", "aac", "-b:a", "192k",
                    "-shortest", "-movflags", "+faststart", mp4], check=True)
    print("trailer:", mp4, "%.1fs" % total)


def load_wav(path):
    with wave.open(path) as w:
        sr = w.getframerate()
        data = np.frombuffer(w.readframes(w.getnframes()), np.int16).astype(np.float32) / 32768.0
    return data, sr


def build_audio(cap, timeline, total, out):
    sr = 32000
    n = int(total * sr) + sr
    mix = np.zeros(n, np.float32)
    adir = os.path.join(cap, "audio")
    clips = {}
    for f in os.listdir(adir):
        d, csr = load_wav(os.path.join(adir, f))
        clips[f[:-4]] = d
    def place(name, t, gain, length=None, fade=0.0):
        if name not in clips:
            return
        d = clips[name]
        s = int(t * sr)
        if length is not None:
            L = int(length * sr)
            reps = int(np.ceil(L / len(d))) + 1
            d = np.tile(d, reps)[:L].copy()
            fl = int(fade * sr)
            if fl > 0:
                d[:fl] *= np.linspace(0, 1, fl)
                d[-fl:] *= np.linspace(1, 0, fl)
        e = min(n, s + len(d))
        mix[s:e] += d[: e - s] * gain
    # music bed: group consecutive shots that share a track, crossfade between groups
    groups = []
    for name, start, cnt in timeline:
        trk = MUSIC.get(name, "music_calm")
        t0, t1 = start / FPS, (start + cnt) / FPS
        if groups and groups[-1][0] == trk:
            groups[-1][2] = t1
        else:
            groups.append([trk, t0, t1])
    for trk, t0, t1 in groups:
        a = max(0, t0 - 0.8)
        place(trk, a, 0.75, length=(t1 - a) + 0.8, fade=0.9)
    place("ocean", 0, 0.18, length=total, fade=1.5)
    starts = {name: start / FPS for name, start, cnt in timeline}
    lens = {name: cnt / FPS for name, start, cnt in timeline}
    if "01_muziris_dawn" in starts: place("gong", starts["01_muziris_dawn"] + 0.5, 0.5)
    for key in ("04_open_sea_day", "05_storm", "08_era_shift", "10_ras_al_mira"):
        if key in starts: place("stinger", starts[key] + 0.15, 0.55)
    if "05_storm" in starts:
        place("thunder", starts["05_storm"] + 1.2, 0.9)
        place("thunder", starts["05_storm"] + 4.6, 0.7)
    if "06_battle" in starts:
        place("conch", starts["06_battle"], 0.8)
        t = starts["06_battle"]
        while t < starts["06_battle"] + lens["06_battle"]:
            place("volley", t, 0.55)
            t += 1.6
        place("crash", starts["06_battle"] + 5.0, 0.6)
    if "08_era_shift" in starts: place("shift_on", starts["08_era_shift"] + lens["08_era_shift"] * 0.2, 0.9)
    if "11_finale" in starts:
        place("discovery", starts["11_finale"] + 0.8, 0.8)
        place("gong", starts["11_finale"] + 3.2, 0.6)
    # master fade + normalise
    fo = int(2.0 * sr)
    end = int(total * sr)
    mix[end - fo:end] *= np.linspace(1, 0, fo)
    mix[end:] = 0
    mix /= max(1e-6, np.max(np.abs(mix))) / 0.9
    path = os.path.join(out, "_trailer_audio.wav")
    with wave.open(path, "w") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes((mix[:end] * 32767).astype(np.int16).tobytes())
    return path


# --------------------------------------------------------------------------- stills
def fit(img, size):
    W, H = size
    r = max(W / img.width, H / img.height)
    img = img.resize((int(img.width * r + 0.5), int(img.height * r + 0.5)), Image.LANCZOS)
    x, y = (img.width - W) // 2, (img.height - H) // 2
    return img.crop((x, y, x + W, y + H))


def thumbnail(still, out_path, variant):
    W, H = 1280, 720
    img = fit(Image.open(still).convert("RGBA"), (W, H))
    img = Image.alpha_composite(img, left_shade((W, H), 0.7, 0.9))
    img = Image.alpha_composite(img, vignette((W, H), 0.45))
    if variant == 0:
        img = draw_text(img, "THE SPICE", font("georgiab.ttf", 118), GOLD, xy=(54, 150), stroke=3)
        img = draw_text(img, "ROUTE", font("georgiab.ttf", 150), GOLD, xy=(50, 270), stroke=3)
        img = draw_text(img, spaced("SAIL  ·  TRADE  ·  SHIFT TIME"), font("seguisb.ttf", 30), PARCH, xy=(60, 470))
        badge = Image.new("RGBA", (W, H), (0, 0, 0, 0))
        ImageDraw.Draw(badge).rounded_rectangle([58, 540, 470, 610], 14, fill=(10, 30, 32, 220), outline=TEAL + (255,), width=3)
        img = Image.alpha_composite(img, badge)
        img = draw_text(img, "FREE BROWSER GAME", font("seguisb.ttf", 32), TEAL, center_xy=(264, 575), shadow=False)
    else:
        img = draw_text(img, "ERA", font("georgiab.ttf", 150), TEAL, xy=(54, 120), stroke=3)
        img = draw_text(img, "SHIFT", font("georgiab.ttf", 150), GOLD, xy=(50, 270), stroke=3)
        img = draw_text(img, "The Spice Route  ·  a Kerala sea adventure", font("georgia.ttf", 34), PARCH, xy=(60, 470))
        img = draw_text(img, spaced("GAME OFF 2026"), font("seguisb.ttf", 28), PARCH, xy=(60, 540))
    img.convert("RGB").save(out_path, quality=95)


def itch_cover(still, out_path):
    W, H = 630, 500
    img = fit(Image.open(still).convert("RGBA"), (W, H))
    img = Image.alpha_composite(img, bottom_shade((W, H), 0.55, 0.92))
    img = Image.alpha_composite(img, vignette((W, H), 0.4))
    img = draw_text(img, "THE SPICE ROUTE", font("georgiab.ttf", 50), GOLD, center_xy=(W / 2, H - 132), stroke=1)
    img = draw_text(img, spaced("ACROSS SEVEN SEAS"), font("seguisb.ttf", 20), PARCH, center_xy=(W / 2, H - 84))
    img = draw_text(img, "Chapter I  —  The Malabar Expedition", font("georgia.ttf", 20), PARCH, center_xy=(W / 2, H - 50))
    img.convert("RGB").save(out_path, quality=95)


def banner(still, out_path):
    W, H = 1920, 480
    img = fit(Image.open(still).convert("RGBA"), (W, H))
    img = Image.alpha_composite(img, left_shade((W, H), 0.55, 0.85))
    img = draw_text(img, spaced("THE SPICE ROUTE"), font("georgiab.ttf", 92), GOLD, xy=(80, 150), stroke=2)
    img = draw_text(img, spaced("ACROSS SEVEN SEAS  ·  GAME OFF 2026"), font("seguisb.ttf", 30), PARCH, xy=(86, 280))
    img.convert("RGB").save(out_path, quality=95)


def main():
    cap, auto, out = sys.argv[1], sys.argv[2], sys.argv[3]
    os.makedirs(out, exist_ok=True)
    stills = os.path.join(cap, "stills")
    shots = os.path.join(out, "itch_screenshots")
    os.makedirs(shots, exist_ok=True)

    hero = os.path.join(stills, "hero_sunset_close.png")
    hero2 = os.path.join(stills, "08_era_shift.png")
    thumbnail(hero, os.path.join(out, "youtube_thumbnail_A_1280x720.jpg"), 0)
    thumbnail(hero2 if os.path.exists(hero2) else hero, os.path.join(out, "youtube_thumbnail_B_1280x720.jpg"), 1)
    itch_cover(os.path.join(stills, "07_sunset_echoes.png") if os.path.exists(os.path.join(stills, "07_sunset_echoes.png")) else hero,
               os.path.join(out, "itch_cover_630x500.jpg"))
    banner(os.path.join(stills, "hero_dawn_close.png"), os.path.join(out, "itch_banner_1920x480.jpg"))

    i = 1
    for name in ["01_muziris_dawn", "03_sail_dawn", "05_storm", "06_battle", "08_era_shift", "07_sunset_echoes", "11_finale", "hero_sunset_close", "hero_dawn_close"]:
        p = os.path.join(stills, name + ".png")
        if os.path.exists(p):
            Image.open(p).convert("RGB").resize((1920, 1080), Image.LANCZOS).save(os.path.join(shots, "%02d_%s.jpg" % (i, name)), quality=93)
            i += 1
    if auto and os.path.isdir(auto):
        wanted = ["market_open", "step2", "step3", "step4", "step6", "step7", "ending_screen"]
        files = sorted(os.listdir(auto))
        for w in wanted:
            pick = [f for f in files if f.endswith(".png") and w in f]
            if pick:
                Image.open(os.path.join(auto, pick[len(pick) // 2])).convert("RGB").resize((1920, 1080), Image.LANCZOS).save(
                    os.path.join(shots, "%02d_gameplay_%s.jpg" % (i, w)), quality=93)
                i += 1
    build_trailer(cap, out)


if __name__ == "__main__":
    main()
