# itch.io page setup: The Spice Route

Everything here is themed on the game's palette: night-sea black, parchment, monsoon gold and Era Shift teal.

## 1. Basic info (Edit game)

| Field | Value |
| --- | --- |
| Title | The Spice Route: Across Seven Seas |
| Short description / tagline | Sail Kerala's ancient spice route across four chapters: trade, explore on foot, build your crew, and SHIFT through time. |
| Classification | Games |
| Kind of project | HTML |
| Release status | Released (all four chapters are in the build) |
| Pricing | No payments (or "$0 or donate") |
| Uploads | `SpiceRoute-itchio-webgl.zip`, tick **This file will be played in the browser** |
| Genre | Adventure |
| Tags (up to 10) | `sailing`, `3d`, `atmospheric`, `historical`, `exploration`, `trading`, `story-rich`, `multiple-endings`, `india`, `game-off` |
| AI generation disclosure | Answer honestly according to how the game was made |
| Community | Comments on |
| Visibility | Public, when ready |

Submit the game to the **Game Off 2026** jam page afterwards, so it appears in the jam entries.

## 2. Embed options

| Setting | Value |
| --- | --- |
| Viewport dimensions | **1280 × 720** |
| Fullscreen button | **On** |
| Mobile friendly | Off (needs keyboard and mouse) |
| Automatically start on page load | Off (gives a "Run game" button, so the page loads faster) |
| Scrollbars | Off |
| Orientation | Landscape |
| SharedArrayBuffer support | Off |

## 3. Cover image, screenshots and trailer

| Item | File |
| --- | --- |
| Cover image (630 × 500) | `Media/out/itch_cover_630x500.jpg` |
| Gameplay video / trailer | Upload `Media/out/SpiceRoute_Trailer_1080p_web.mp4` to YouTube, then paste the YouTube link |
| Screenshots (pick 8–12) | `Media/out/itch_screenshots/`. Suggested order: 01 Muziris dawn, 05 Era Shift, 03 storm, 20 fog beacons, 21 Gate of Tears, 22 Black Compass, 23 canal tide, 24 Alexandria night, 26 Guild fleet, 27 Storm Wall, 29 Last Map, 16 ashore, 17 skills, 18 crew, 30 the choice |

## 4. Theme editor ("Edit theme" button on the game page)

### Images
| Slot | File | Setting |
| --- | --- | --- |
| Banner | `banner_960x360.jpg` | Banner replaces the title text, so it stays readable |
| Background | `background_2560x1600.jpg` | **Don't repeat**, **Centered**, **Fixed** if offered |

### Colors
| Slot | Hex | What it is |
| --- | --- | --- |
| Background (BG) | `#070B0F` | night-sea black, matches the background image's lower half |
| Background 2 / inner column (BG2) | `#0E151B` | slightly lifted panel; set opacity to around 90% if available |
| Text | `#E9DCC0` | parchment |
| Link | `#E8B95A` | monsoon gold |
| Button | `#C8862E` | burnished gold (button text turns dark automatically) |
| Border / embed background, if offered | `#1C2730` | |

Alternative "Era Shift" accent: use `#78E1D7` (teal) for **Link** instead of gold for a cooler, mysterious look.

### Fonts (Google fonts in the theme editor)
| Slot | First choice | Fallback |
| --- | --- | --- |
| Header font | **Cinzel** (bold) | Playfair Display, then Lora |
| Body font | **Lora** | Crimson Text, then Merriweather |
| Header size | Large | |
| Body size | 16–17 px | |

### Layout
- Column layout: **single column, wide** (fits the 960-px art), or the default two-column with screenshots on the right.
- Header alignment: center.
- "Hide title" (when using the banner): on.

## 5. Description

Open `page_description.html`, upload each image in the itch description editor, swap every `UPLOAD:filename` for the URL itch gives you, then paste it in the editor's HTML mode. If you prefer the visual editor, paste the text and insert the images in this order:

1. `logo_transparent_960x300.png`, tagline, `gif_sailing.gif`
2. `divider_the_voyage.png`, story text
3. `divider_four_chapters.png`, `four_chapters_960.png`, chapter list, then `gif_fog_beacons.gif`, `gif_stealth.gif`, `gif_canal_tide.gif`, `gif_pharos_night.gif`, `gif_storm_wall.gif`, `gif_archive.gif`
4. `divider_seven_shifts.png`, `seven_shifts_960.png`, shift list, `gif_era_shift.gif`
5. `divider_land_crew_ship.png`, `land_crew_ship_960.png`, feature list, `gif_ashore.gif`
6. `divider_how_to_play.png`, `controls_960.png`, tips
7. `gif_storm.gif`, `gif_flagship.gif`, `gif_fleet.gif`, `gif_last_map.gif`
8. `divider_features.png`, feature list
9. `divider_made_in_code.png`, tech text, `gif_sunset.gif`
10. `divider_credits.png`, credits (fill in your name)

The page uses 15 GIFs (about 55 MB in total). If it loads slowly, drop some: the page reads fine with one GIF per section. `gif_battle.gif` (the Chapter I raider fight) is included in the folder as a spare.

The easiest route is `page_description_github.html`: its images are already hosted in the GitHub repo, so you can paste it straight into the editor's HTML mode with nothing to upload.

## 6. Files in this folder

| File | Size | Use |
| --- | --- | --- |
| `background_2560x1600.jpg` | 2560 × 1600 | page background (sunset sky fading into night sea) |
| `banner_960x360.jpg` | 960 × 360 | theme banner |
| `logo_transparent_960x300.png` | 960 × 300, transparent | top of description, or banner over the background |
| `divider_*.png` (8) | 960 × 110, transparent | gold ornamental section headers |
| `seven_shifts_960.png` | 960 × 414 | the Seven Shifts infographic (all seven) |
| `four_chapters_960.png` | 960 × 702 | one picture and caption per chapter |
| `land_crew_ship_960.png` | 960 × 702 | going ashore and the three Captain's Ledger pages |
| `controls_960.png` | 960 × 992 | controls card: at sea, on land, anywhere |
| `gif_sailing`, `gif_storm`, `gif_battle`, `gif_era_shift`, `gif_sunset` | 630 px wide, 3–5 MB | Chapter I moments |
| `gif_fog_beacons`, `gif_stealth`, `gif_flagship` | 600 px wide, about 4 MB | Chapter II: fog beacons, the night strait, the Black Compass |
| `gif_canal_tide`, `gif_pharos_night` | 600 px wide, about 3 MB | Chapter III: the canal, Alexandria at night |
| `gif_fleet`, `gif_storm_wall`, `gif_archive`, `gif_last_map` | 600 px wide, about 4 MB | Chapter IV: the Guild fleet, the Storm Wall, the Archive, the final battle |
| `gif_ashore` | 600 px wide, 3 MB | walking ashore in Muziris |
| `page_description_github.html` | | full page, images hosted on GitHub: paste and go |
| `page_description_github.md` | | the same page as Markdown |
| `page_description.html` | | the same page with local file names, if you upload the images to itch yourself |
| `preview_itch_page.html` | | open in a browser to preview the finished page |

## 7. Optional custom CSS

itch.io only allows custom CSS on accounts that have asked support to enable it. If yours has it:

```css
.inner_column { box-shadow: 0 0 60px rgba(0,0,0,0.7); border: 1px solid rgba(232,185,90,0.25); }
.formatted_description img { display: block; margin: 0 auto; max-width: 100%; }
.formatted_description h2, .formatted_description h3 { letter-spacing: 0.18em; color: #E8B95A; text-align: center; }
.button { letter-spacing: 0.15em; text-transform: uppercase; }
.game_frame { border: 1px solid rgba(232,185,90,0.35); box-shadow: 0 0 40px rgba(232,185,90,0.12); }
```

To regenerate the images and GIFs: `python Media/make_itch_page.py`. To regenerate the page text: `python Media/make_page_text.py`.
