# Graphics (NARC backgrounds and sprites)

Many labels are pictures, not text. Examples:
- the top screen of MISSION SELECT: ■ 情勢変化予想 ■, ユニオン / 人革連, 影響度, ミッション 内容;
- the pilot stats;
- the button config panels;
- the result screen.

They live in the 2D/*.narc archives (300 archives in the ROM). The ROM browser shows them under **Graphics**. Each archive opens with a list of its backgrounds and sprite cells, with preview, Export PNG, Import PNG and Apply to ROM.

## Formats

| File | Chunk | Content |
|---|---|---|
| NARC | `BTAF` / `BTNF` / `GMIF` | file offsets / file names (folder `dst/`) / file data |
| NCLR | `TTLP` | palette, BGR555 (5 bits per channel) |
| NCGR | `RAHC` | 8×8 tiles, 4 bpp (16 colors, map chooses one of 16 palettes) or 8 bpp (256 colors) |
| NSCR | `NRCS` | screen map, one u16 per 8×8 cell: tile (bits 0–9), h-flip (10), v-flip (11), palette (12–15) |
| NCER | `KBEC` | sprite cells: cell count, bank type (1 = 16-byte entries), data offset, mapping shift; each cell = list of OAMs (3 × u16, DS OAM attributes) |
| NANR | `KNBA` | animations (which cell when), not edited |

- **Backgrounds:** a background is the NSCR + NCGR + NCLR with the same name. The maps are 256 × 256 px; the DS shows the top 256 × 192.
- **Sprites:** a sprite set is an NCER with the NCGR of the same name, and the NCLR of the same name (or the first one in the archive).
  - OAM tile number × 2^mapping × 32 = byte offset in the NCGR (1D mapping).
  - 8-bit OAMs use 256-color (extended) palettes.

## Export / import (`Features/Graphics`)

### Backgrounds

- **Export:** renders the map to a PNG. Color index 0 is transparent; the game shows the backdrop there.
- **Import:** every 8×8 cell is matched to the **nearest colors of the original palette**. 4-bit backgrounds pick the best of their 16 palettes per cell. Identical and mirrored tiles are shared.
  - The palette is never changed.
  - NCGR and NSCR keep their sizes and are patched in place inside the NARC.
  - An image that needs more different tiles than the NCGR holds is refused with a message.

### Sprite cells

- **Export:** renders the cell (its bounding box). The first OAM of a cell is drawn on top.
- **Import:** only **changed** pixels are written. Each one goes into the tiles of the **front-most OAM** covering it, as the nearest color of that OAM's palette.
  - A pixel made transparent is cleared in every OAM covering it.
  - Unchanged pixels keep their bytes, so tiles shown with other palettes stay as they were (e.g. a selected button is the same tiles with an orange palette).
- **Shared tiles:**
  - Cells share tiles: animation frames, selected states. Changing one cell changes all of them, which is wanted.
  - Plates and frames are often a few tiles used many times. Text usually is an OAM of its own in front of them. New text must stay inside the area of the old text's OAMs; otherwise it lands in the shared plate tiles and appears on every plate.
  - Examples: the button config panels (`KEY_OBJ`), where words like ロックオン are separate OAMs and the blue plates are shared; the pilot select buttons.

### Checked on the original ROM

All of these come back byte-identical when unchanged and pixel-identical after export → import:
- 336 backgrounds;
- 67 sprite sets with 4779 cells.

## Translated screens (test ROM, October 2026)

| Archive | Item | Screen / text |
|---|---|---|
| Briefing, Common, LogueCommon | `Briefing2_2B_BG` | MISSION SELECT top: SITUATION FORECAST, UNION, HRL, IMPACT, Mission Details |
| Briefing | `Briefing2_4_BG` | pilot select top: BATTLE INFO, TAC. SUPPORT |
| Briefing | `Briefing_2_3BG` | pilot stats: SHOOTING, MELEE, DEFENSE, MOBILITY, TYPE, STRONG VS. (the values 格闘 / ＡＥＵ are program texts, see [arm9](../arm9/README.md)) |
| Briefing | `KEY_OBJ` #0–#3, `KEY_CONFIG_A` #120 | button config: LOCK ON DEF. TARGET, LOCK ON, GUARD, UP, DOWN, MELEE, FIRE, MOVE, TRANSFORM (KYRIOS), TYPE A–D |
| Briefing, Common | `Briefing_2_3OBJ` #0, `Brifing_A00`/`Brifing_B00` #8 | R BUTTON (map switch) |
| Briefing | `SELECT_MOBJ00` #88 | A: CONFIRM |
| Briefing | `Mission_Obj01` #163 | LEVEL (under "Difficulty") |
| Briefing | `Pilot_Select01`–`04` #113, #120, #121, #128, #130 | CAN'T LAUNCH, LAUNCH, SUPPORT, pilot / Gundam captions |
| Result | `Result_B_OBJ` #36, `Result_Plate00` #9 | BATTLE REPORT, UNION, HRL, STRENGTH, MATCHUP, DIFFICULTY, MORALE, TOTAL pts, A: NEXT |
| TacticalSupport | `Card_A00`–`L00` | card name banners |
| TacticalSupport | `Card_Obj_00` #0/#2, `GN_Card_Obj_00` #108/#114/#149/#150/#151 | AVAILABLE, READY IN … DAYS, SET, UNAVAILABLE, B: BACK, A: SELECT/REMOVE, Tactical supports usable per mission |
| ZZZ_TITLE | `Trial_TITLE_Button` #0 | PRESS A TO START (trial build title) |
| MAIN_MENU | `MAIN_MENU_00` #0, #19–#26, `MAIN_MENU_BG00` | trial build menu: STORY MODE, WIRELESS, GUNPLA NAVI, OPTIONS, notice |

**Left as they are:**
- The 機動戦士ガンダム00 logo art and © line.
- Photographed product boxes in the Gunpla photos (ET/FG/HG/HCM/GUN_*).
- The Gunpla Navi archives (GPN_Common, GunplaNavi, GUN_LV*), excluded on request.

**Pictures without Japanese:** the staff roll, epilogue and prologue images.

**Layout limits on the mission details screen:** the faction icons and influence dashes are sprites drawn over `Briefing2_2B_BG`.
- Faction names are centred right of the icons (x ≈ 74).
- The 影響度 label must end before x 163, so "IMPACT" fits but "INFLUENCE" does not.

## How the English images were made

The test pictures were painted by the harness from a JSON spec (`claudetemp/gfx_spec.json`). Per item, each label gives:
- the area;
- how the old text is removed (`col:X` copies a clean column per row, `lerp:X0,X1` blends a horizontal gradient, `clear` makes it transparent, `color:#hex`);
- the English text, font, colors and outline.

The resulting `archive__item.png` files (in `claudetemp/gfx`) are what a user would import in the app.