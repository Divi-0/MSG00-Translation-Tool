# Game font and extra characters

The game draws all texts (conversations, prologue, missions, menus) with one Nitro font, `LC_font_12.NFTR`. The font has only the characters of Shift-JIS, so letters like ä, é or ñ are missing. The tool adds them as **extra characters** ("Font › Extra characters" in the ROM tree, `Features/Font`).

## The font

- 11 copies, one in each of these NARCs: `2D/Common`, `Briefing`, `GPN_Common`, `GunplaNavi`, `INDICATOR`, `LogueCommon`, `PAUSE`, `Result`, `SaveLoad`, `TacticalSupport`, `ZZZ_TITLE`. Each screen uses the copy of its own archive, so all copies are changed together. The ARM9 program has no font, only the code that checks the `RTFN` magic.
- NFTR 1.1, Shift-JIS (encoding 2), 3568 glyphs (GunplaNavi: 3561), cells of 11 × 11 px at 1 bpp, 16 bytes per cell.
- 1-byte characters (ASCII and half-width katakana) are 6 px wide (advance 6, the letter is usually in columns 1–5). Full-width characters are 12 px wide.
- Rows: capitals use 0–9, lowercase letters 3–9, descenders reach row 10.

## Extra characters

- Each extra character replaces a **half-width katakana** (Shift-JIS bytes `0xA1`–`0xDF`). In the texts it is 1 byte, and on screen it is as wide as a letter. The existing line limits (bytes per line) stay correct.
- `0xA5` (`･`, half-width middle dot) is used once by the Japanese cut-ins and stays reserved. That leaves **62 slots**. The rest are not used by any original text (checked on all exports in `translation/jap`).
- Characters that ASCII or Shift-JIS already have are refused. Shift-JIS characters would be full-width, and mapping them to a slot would change existing Japanese text.
- **Presets** (`CharacterPresets`) are German, Spanish, French, Italian and Portuguese, plus "Western European", which has all 55 of them. Picking a preset only adds its characters to the editor.
- **Own characters**: type or paste them into the editor. Each one gets the next free slot.
- **Glyphs** start as a draft (`GlyphFactory`):
  - accented letters are the font's own letter plus the accent; capitals and tall letters are squeezed from 10 to 8 rows to make room, and i/j lose their dot;
  - ¡ and ¿ are ! and ? turned upside down;
  - ß, Œ, œ, Æ, æ, Ø, ø, Ł, ł, Đ, đ, «, » and € are drawn in the code;
  - anything else starts empty and is drawn on the 6 × 11 grid of the editor.
- **Nothing is written** until "Apply to ROM". Applying writes the glyphs and the character table into all 11 copies, in place (no file changes size). From then on every reader, writer and editor uses the characters.
- Export/Import saves the characters with their pixels as JSON, to share them between translators.

## Where the table is kept

The table (which slot holds which character) is stored in the ROM itself, in the **spare bits of the glyph cells**. An 11 × 11 glyph at 1 bpp uses 121 of the 128 bits of its 16-byte cell, and the game never draws the other 7. All of these spare bits are zero in the original ROM.

Format, written bit by bit from glyph 0 on, most significant bit first: `"MSGC"`, version `1`, count, then for each character: slot byte, UTF-16 code (little endian). That is 6 + 3 × count bytes, at most 192, out of about 3 KB available. A ROM without the table (the original) has no extra characters.

When a ROM is opened, `IFontService.LoadCharacterTable()` reads the table from `2D/Common.narc` and sets it on `GameEncoding.Instance`. That is the encoding of all game texts: Shift-JIS plus the extra characters. Lead bytes (`0x81`–`0x9F`, `0xE0`–`0xFC`) always start a 2-byte character, so a trail byte such as the `A1` in `82 A1` (ぃ) is never read as an extra character.

## Characters missing from the font

Plain Shift-JIS writes a look-alike for a character it does not have (ä → a) or `?`, with no error. The editors therefore warn when a text has characters that are neither in the font nor extra characters ("Not in the game font: …").

## Tested

BizHawk, test ROM built from the original with the Western European preset (2026-10-14):

- the first prologue text with all 55 characters, centered on a 42-byte line, through LogueCommon's copy of the font;
- the MODE SELECT help texts (ARM9) through ZZZ_TITLE's copy.

Every character showed at 6 px, the spare-bit table caused no visible pixels, and the saved ROM read back with the same table and texts.