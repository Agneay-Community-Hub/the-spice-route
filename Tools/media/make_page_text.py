"""
Writes the itch.io page description in every format from one source:

    itch_page/page_description_github.md     Markdown, images hosted on GitHub
    itch_page/page_description_github.html   HTML, images hosted on GitHub (paste into itch's HTML mode)
    itch_page/page_description.html          HTML, local file names (upload the images to itch yourself)
    itch_page/preview_itch_page.html         local preview of the finished page

    python make_page_text.py
"""
import os
import re

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "itch_page")
RAW = "https://raw.githubusercontent.com/Agneay-Community-Hub/the-spice-route/main/docs/itch/"

# ("img", file, alt)  ("c", centred paragraph)  ("p", paragraph)  ("ul", [items])  — text uses **bold** and *italic*
PAGE = [
    ("img", "logo_transparent_960x300.png", "The Spice Route: Across Seven Seas"),
    ("c", "*\"The sea remembers every journey.\"*"),
    ("c", "**Sail an ancient stitched-plank trading ship out of Muziris on the Malabar Coast of Kerala. Trade spices, read the monsoon, slip past watchfires at night, lead a fleet through a storm that never ends, and use your missing father's bronze compass to SHIFT between the present and the sea's memory of the past.**"),
    ("c", "A GitHub Game Off 2026 entry  ·  Theme: **SHIFT**  ·  Plays free in your browser  ·  four chapters, about 40 minutes"),
    ("img", "gif_sailing.gif", "Sailing out of Muziris at dawn"),

    ("img", "divider_the_voyage.png", "The Voyage"),
    ("p", "Seven years ago the navigator **Raghavan** sailed west aboard the *Vellimeen*, searching for a route no chart recorded. He never returned."),
    ("p", "His son **Arivan** inherited three things: a battered ship named the *Kaveri*, his father's bronze compass, and a promise to find the truth. It begins with a sealed package that must cross the western sea before the monsoon shifts. It ends beyond the edge of every known map."),

    ("img", "divider_four_chapters.png", "Four Chapters"),
    ("img", "four_chapters_960.png", "The four chapters of The Spice Route"),
    ("ul", [
        "**I · The Malabar Expedition.** Fill your hold in Muziris, survive a monsoon squall, fight off a raider and read the echoes of a lost expedition.",
        "**II · The Gate of Tears.** Follow ancient beacons through a fog-bound reef coast, trade in Aden, slip through a guarded strait at night with your lantern doused, and face the flagship *Black Compass*.",
        "**III · The Great Marketplace.** Sail the silted canal of the old kings on the rising tide, sell your cargo in Alexandria, and free a prisoner under the sweeping beam of the Pharos lighthouse.",
        "**IV · Across Seven Seas.** Lead a Guild fleet through the Storm Wall to the Sunken Archive, sink *The Last Map*, and choose one of **four endings**.",
    ]),
    ("img", "gif_fog_beacons.gif", "Era Shift reveals the ancient beacons in the fog"),
    ("img", "gif_stealth.gif", "Slipping through the Gate of Tears at night"),
    ("img", "gif_canal_tide.gif", "The canal of the old kings"),
    ("img", "gif_pharos_night.gif", "Under the beam of the Pharos lighthouse"),
    ("img", "gif_storm_wall.gif", "The Storm Wall"),
    ("img", "gif_archive.gif", "Reading the echo of the Sunken Archive"),

    ("img", "divider_seven_shifts.png", "The Seven Shifts"),
    ("img", "seven_shifts_960.png", "Wind, Course, Tide, Weather, Perspective, Trade and Era Shift"),
    ("ul", [
        "**Wind Shift**: the monsoon turns mid-voyage. A square sail needs the wind behind or beside it.",
        "**Course Shift**: abandon your heading and run for shelter when the weather turns.",
        "**Tide Shift**: the sea rises and falls. Sandbars ground you at low tide, so cross at high water.",
        "**Weather Shift**: monsoon squalls, sea fog, night, and a storm that never ends.",
        "**Perspective Shift**: stand at the helm, or leave the ship and walk the shore.",
        "**Trade Shift**: what is cheap in one port is worth three times as much in the next.",
        "**Era Shift**: your father's compass shows the sea as it once was: ghost harbours, lost beacons, sunken cities and doors that only exist in the past.",
    ]),
    ("img", "gif_era_shift.gif", "Era Shift reveals the ghost harbour"),

    ("img", "divider_land_crew_ship.png", "Land, Crew and Ship"),
    ("img", "land_crew_ship_960.png", "Going ashore and the Captain's Ledger"),
    ("ul", [
        "**Go ashore.** Stop near any beach or port and press **F**. Walk to the light beams: gold beams are shops, teal beams are discoveries.",
        "**Find things.** Inscriptions give skill points, chests and wild spices give gold, and sealed vaults open only through the Era Shift.",
        "**Three skill trees.** Navigator, Merchant and Warrior, with four skills each.",
        "**Upgrade your ship.** Hull, sails, steering oar, hold and fire-arrows, each with three levels you can see on the ship.",
        "**Five kinds of ship.** Refit the *Kaveri* as a Swift Merchant, Great Cargo Ship, War Galley or Explorer.",
        "**A crew with loyalty.** Hire up to six sailors, give each a station, and keep them paid. Loyal sailors work harder; unpaid ones leave.",
    ]),
    ("img", "gif_ashore.gif", "Walking ashore in Muziris"),

    ("img", "divider_how_to_play.png", "How to Play"),
    ("p", "**New to games? Choose TUTORIAL VOYAGE on the title screen.** A lesson panel teaches every button as you play, and a help line always tells you the best next move. The tutorial also turns on assists: lighter damage, a stronger monsoon and gentle bumps instead of crashes."),
    ("img", "controls_960.png", "Controls"),
    ("p", "**Tips**"),
    ("ul", [
        "Click the game once to take the helm. Your mouse then moves the camera.",
        "The **gold diamond** always shows where to go. Press **T** and your crew will steer there for you.",
        "Story goals are reached by stopping *near* them on the water. Going ashore with **F** is always optional, and always worth it.",
        "Your archers fire sideways: put the enemy *beside* your ship, then click.",
        "Your hull repairs itself after 8 seconds without being hit.",
        "Press **TAB** whenever you earn skill points. A stronger ship makes the flagships much easier.",
    ]),
    ("img", "gif_storm.gif", "The monsoon squall"),
    ("img", "gif_flagship.gif", "Battle with the Black Compass"),
    ("img", "gif_fleet.gif", "The Guild fleet follows the Kaveri"),
    ("img", "gif_last_map.gif", "The final battle"),

    ("img", "divider_features.png", "Features"),
    ("ul", [
        "A complete four-chapter story across the ancient Indian Ocean, from Kerala to Arabia, the Red Sea and Alexandria, with four endings",
        "Arcade-sim sailing: square-sail physics, steering oar, monsoon winds, tides and wave buoyancy",
        "On-foot exploration of ports and islands, with shops, treasure and Era Shift vaults",
        "Skill trees, ship upgrades, ship classes and a crew whose loyalty you have to earn",
        "Fire-arrow broadsides, ramming, boss flagships, lantern stealth and allied ships that fight beside you",
        "Trading with different goods and prices in every port",
        "Dawn, midday, storm, fog, sunset and night",
        "A fully guided Tutorial Voyage for first-time players",
        "Original music inspired by Kerala traditions: tanpura drone, a bansuri-style flute in Raga Mohanam, chenda and mizhavu drums, and a conch horn",
    ]),

    ("img", "divider_made_in_code.png", "Made entirely in code"),
    ("p", "Built in **Unity 6** for the web. **No imported assets**: every ship, palm tree, Kerala house, temple, lighthouse and island, every texture, sound effect and note of music is generated by code when the game loads. Custom shaders draw the Gerstner-wave ocean with shore foam and ship wakes, the sky with clouds, stars and lightning, the billowing sails and the golden Era Shift memory effect."),
    ("img", "gif_sunset.gif", "Sunset at the Isle of Echoes"),

    ("img", "divider_credits.png", "Credits"),
    ("p", "Design, code, art, music and sound: **[YOUR NAME]**"),
    ("p", "Source code: [GitHub](https://github.com/Agneay-Community-Hub/the-spice-route)"),
    ("p", "The setting, characters and events are fictional, inspired by the historical trade between the Malabar Coast and the western world."),
]


def to_html(s):
    s = re.sub(r"\[([^\]]+)\]\((https?://[^)]+)\)", r'<a href="\2">\1</a>', s)
    s = re.sub(r"\*\*(.+?)\*\*", r"<strong>\1</strong>", s)
    s = re.sub(r"\*(.+?)\*", r"<em>\1</em>", s)
    return s


def html(prefix):
    out = []
    for b in PAGE:
        if b[0] == "img":
            out.append('<p style="text-align:center"><img src="%s%s" alt="%s"></p>' % (prefix, b[1], b[2]))
        elif b[0] == "c":
            out.append('<p style="text-align:center">%s</p>' % to_html(b[1]))
        elif b[0] == "p":
            out.append("<p>%s</p>" % to_html(b[1]))
        else:
            out.append("<ul>\n" + "\n".join("<li>%s</li>" % to_html(i) for i in b[1]) + "\n</ul>")
    return "\n\n".join(out) + "\n"


def markdown():
    out = []
    for b in PAGE:
        if b[0] == "img":
            out.append("![%s](%s%s)" % (b[2], RAW, b[1]))
        elif b[0] in ("c", "p"):
            out.append(b[1])
        else:
            out.append("\n".join("- " + i for i in b[1]))
    return "\n\n".join(out) + "\n"


PREVIEW_HEAD = """<!DOCTYPE html>
<html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>The Spice Route: itch.io page preview</title>
<link href="https://fonts.googleapis.com/css2?family=Cinzel:wght@600;800&family=Lora:ital,wght@0,400;0,600;1,400&display=swap" rel="stylesheet">
<style>
:root{--bg:#070B0F;--bg2:#0E151B;--text:#E9DCC0;--link:#E8B95A;--button:#C8862E}
html,body{margin:0;background:var(--bg);color:var(--text);font-family:Lora,Georgia,serif;font-size:16.5px;line-height:1.6}
body{background:var(--bg) url("background_2560x1600.jpg") no-repeat center top fixed;background-size:cover}
.wrap{max-width:960px;margin:0 auto;padding:40px 16px 80px}
.banner img{display:block;width:100%;height:auto}
.col{background:rgba(14,21,27,.92);border:1px solid rgba(232,185,90,.25);box-shadow:0 0 60px rgba(0,0,0,.7);margin-top:18px}
.game{aspect-ratio:16/9;background:#000 url("../out/itch_screenshots/09_hero_dawn_close.jpg") center/cover;display:flex;align-items:center;justify-content:center;border-bottom:1px solid rgba(232,185,90,.35)}
.run{background:var(--button);color:#140c03;font:700 18px Cinzel,serif;letter-spacing:.15em;padding:14px 34px;border-radius:4px;box-shadow:0 0 30px rgba(232,185,90,.4)}
.desc{padding:24px 30px}
.desc img{display:block;margin:0 auto;max-width:100%;height:auto}
.desc a{color:var(--link)}
.desc strong{color:#f3e6c8}
.note{font:13px system-ui,sans-serif;color:#8d8471;text-align:center;margin:10px 0}
@media (max-width:600px){.desc{padding:16px}}
</style></head><body><div class="wrap">
<p class="note">Local preview of the finished itch.io page (theme colours, fonts, background and banner). Not the live page.</p>
<div class="banner"><img src="banner_960x360.jpg" alt="The Spice Route banner"></div>
<div class="col"><div class="game"><span class="run">RUN GAME</span></div>
<div class="desc">

"""


def write(name, s):
    with open(os.path.join(OUT, name), "w", encoding="utf-8", newline="\n") as f:
        f.write(s)


def main():
    missing = [b[1] for b in PAGE if b[0] == "img" and not os.path.exists(os.path.join(OUT, b[1]))]
    if missing:
        print("WARNING, images not generated yet:", ", ".join(missing))
    note = "<!--\n  itch.io page description for The Spice Route (%s).\n  Paste everything below this comment into: Edit game > Description, in the editor's HTML / source mode.\n  Replace [YOUR NAME] in the credits before saving.\n-->\n\n"
    write("page_description_github.md", markdown())
    write("page_description_github.html", note % "images hosted on GitHub" + html(RAW))
    write("page_description.html", note % "local image file names: upload each image to itch and swap in its URL" + html(""))
    write("preview_itch_page.html", PREVIEW_HEAD + html("") + "\n</div></div></div></body></html>\n")
    print("page text written:", sum(1 for b in PAGE if b[0] == "img"), "images referenced")


if __name__ == "__main__":
    main()
