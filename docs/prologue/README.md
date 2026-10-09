# Prologue (MAPI type 0x0A)

Findings from reverse engineering `2D/Prologue.csvb` and testing changes in DeSmuME (October 2026).
Each statement is marked **confirmed** (verified byte-exact or observed in the emulator) or **hypothesis**.

- [File layout](prologue-file-layout.puml) · [Scene flow](prologue-scene-flow.puml)
## Files in the ROM

| ROM path | Content |
|---|---|
| `2D/Prologue.csvb` | the prologue script and all texts, stored uncompressed (3108 bytes) |
| `2D/Pro_01.narc` … `2D/Pro_20.narc` | one image per scene; some include an animation (NANR) |
| `2D/LogueDefine.csvb`, `2D/LogueCommon.narc` | shared prologue/epilogue data, not analysed yet |
| `2D/Epilogue[1-7].csvb`, `2D/Epi_*.narc` | epilogues, same MAPI type `0x0A`, same structure (see [Epilogues](#epilogues-confirmed)) |

## csvb layout (confirmed)

The reader/writer in `Features/Csvb/Mapi/PrologueEpilogue` round-trips `Prologue.csvb` byte-for-byte.

```
0x00  "CSVBv4.3" 01 00 00 00
0x0C  int32 OffsetToEndOfPointerTable   -> points AT the 02/02 marker
0x10  int32 OffsetToEndOfFullHeader     -> marker + 8 = start of text table
0x14  int32 OffsetToEndOfTextTable
0x18  int32 OffsetToEndOfExtraTable     -> start of offset table
0x20  48-byte MAPI header ("MAPI", type at bytes 44..48 = 0x0A)
0x50  pointer table: 8-byte entries {int32 slot offset into offset table, int32 type}
      02 00 00 00 02 00 00 00            (exactly once)
      text table: 00 00 00 00, then NUL-terminated Shift-JIS strings padded to 4 bytes
      extra section (copied verbatim)
      offset table: one 4- or 8-byte slot per pointer group
```

- Entries with the same slot offset form one group (`SubPointer`s in the model) and share one slot.
- Text offsets in the offset table are relative to `OffsetToEndOfFullHeader`.
- Bug fixed: the writer wrote the `02 00 00 00 02 00 00 00` marker twice (8 extra bytes, all header offsets +8). Files written by the old tool, e.g. `prologue_2.csvb`, contain the duplicate.

## Script entries (pointer types)

The pointer table is a script that the game runs from top to bottom. Slot values are split into `lo` (bytes 0..3) and `hi` (bytes 4..7).

| Type | Name in code | Slot | Meaning | Status |
|---|---|---|---|---|
| `0x02` | `ShowImage` | 4 | Frames (60/s) the current image and its animation are shown. Typing of the preceding text starts at the same time. If the value is longer than a looping animation, the animation restarts. | confirmed |
| `0x08` | `Text` | 8 | `lo` = text offset. `hi` = 54/60/72/78 for 6/5/3/2 lines, i.e. `90 − 6 × lines` (probably vertical centering) | `lo` confirmed, `hi` hypothesis |
| `0x00` | `TransitionBefore` | 8 | `lo` = 0 or 2, `hi` = 60/30/18 frames: transition before a text or image | hypothesis |
| `0x01` | `TransitionAfter` | 8 | same shape: transition after an image (fade-out) | hypothesis |
| `0x04`, `0x07`, `0x0A` | `LoadImage`, `SwitchImage`, `Unknown0A` | 8 | two text offsets naming image objects (`pro_02`, `pro_02_obj`) which load `Pro_02.narc` | confirmed (names), hypothesis (loading) |
| `0x09` | `ClearText` | 8 | **clears the text**: the text disappears when a group containing `0x09` is reached; montage groups without `0x09` keep it visible | confirmed (all 11 texts) |
| `0x05`, `0x06` | `SceneChange`, `SceneChangeEnd` | 8 | start/end of a scene-change group | hypothesis |
| `0x03` | `ScriptStart` | 4 | first entry of the script | unknown |
| `0x0B` | `Unknown0B` | 8 | appears only inside the `pro_07` group | unknown |
| `0x0C` | `Unknown0C` | 4 | appears after the last Image (value 120); **not** a wait: adding it after an Image did not extend the scene (test D), and values 434–1310 made the game hang on NEW GAME (test E) | confirmed not a wait |

A typical scene (scene 2):

```
LoadImage [pro_02, pro_02_obj]           load image
TransitionBefore  lo=2 hi=60                transition
Text              "…"                       start typing
ShowImage         480                       show image + text for 480 frames
TransitionAfter   lo=2 hi=60                fade out
SceneChange + SwitchImage [pro_03] + ClearText + SceneChangeEnd
                                            next image; ClearText (0x09) clears the text
```

In montages (texts 6, 7 and 10) several `Image` entries follow one text, separated by groups without `0x09`, so the text stays across quick cuts.

## Timing (confirmed in the emulator)

- The game runs at 60 frames/s. The typewriter needs **~4.1 frames per character**, for Japanese and English alike.
- A text is visible from its `Image` start until the next `0x09` group, i.e. the sum of all its Image values plus the transitions between them.
- English needs far more characters than Japanese, so with the original values the text is cleared before it has finished typing.

Original Japanese prologue:

| Text | Image | Measured on screen |
|---|---|---|
| 1 (6 lines) | 900 | ~15 s (typing ~7 s) |
| 2 | 480 | ~8 s |
| 3 | 300 | ~5 s |

Test variants (English text from `csvb.json`):

| Variant | Change | Result |
|---|---|---|
| A | original values | text cut off while still typing |
| B | duplicate `Image` entry after each text | longer, but the animation plays twice |
| C | `Image` value doubled | longer, but looping animations restart |
| D | extra `0x0C` after the Image | barely any extra time |
| E | larger `0x0C` values | game hangs on NEW GAME |
| F | C + `Pro_01`/`Pro_11` set to play once | longer, animation holds its last frame |
| H | editor's "Use suggested" (see below) + F's animation change | every text typed completely and readable 3–6 s, no loops |

Remaining in H: text 1 readable only ~3 s (typing is a bit slower than 4 frames/char); text 6 only ~2 s, because typing pauses ~3 s while the large `Pro_07` animation loads.

## Animations (confirmed)

The `Pro_XX.narc` archives contain `RGCN` (graphics), `RLCN` (palette) and either `RCSN` (static screen) or `RECN` + `RNAN` (cells + animation).

NANR layout used here: `ABNK` block at the header size (u16 at `0x0C`), base = block + 8:

```
base+0   u16 sequence count, u16 total frames
base+4   u32 sequence table offset, u32 frame table offset, u32 data offset   (relative to base)
sequence (16 bytes): u16 frames, u16 loopStart, u16 frameType, u16 animType, u32 playMode, u32 frameOffset
frame    (8 bytes):  u32 data offset, u16 duration (frames), u16 0xBEEF
playMode: 1 = forward once, 2 = forward loop, 3 = ping-pong once, 4 = ping-pong loop
```

| File | Mode | Length (frames) | Image value (original) |
|---|---|---|---|
| Pro_01 | loop | 1024 (256 × 4) | 900 |
| Pro_02 | loop (2-frame blink overlay) | 60 | 480 |
| Pro_03 / 04 / 05 | once | 15 / 18 / 19 | 300 |
| Pro_07 | once | 386 | 150 + 266 |
| Pro_11 | loop | 512 (256 × 2) | 422 |
| Pro_06, 08–10, 12–20 | static (`RCSN`, no NANR) | – | – |

Changing `playMode` from 2 to 1 (one byte per sequence) makes the animation stop on its last frame. With that, an Image value longer than the animation holds the last frame instead of restarting it. This is required whenever a longer duration is set for `Pro_01` or `Pro_11`.

## Text layout (confirmed)

- The bottom screen is 256 px wide; the font uses **6 px per Shift-JIS byte** (12 px for full-width characters).
- **Max 42 bytes per line**: 42 ASCII characters or 21 full-width characters. The longest original line is exactly 21 full-width characters. English lines of 43–44 characters are cut off at both edges.
- 8 lines (text 1) and 7 lines (text 10) still fit vertically with the original `Text.hi` values.

## Editor support

`MSG00.Translation.UI/Models/PrologueTextEntry.cs` implements the rules above for each text:

- **On screen time** = sum of the text's Images + transitions between them, up to the clearing `0x09` group.
- **Suggested** = `4 × characters + 240` frames (typing + ~3.5 s reading). If the on-screen time is shorter, "Use suggested" adds the missing frames to the **last** Image before the text is cleared, so montages keep their pace.
- **Line warning** for every line over 42 Shift-JIS bytes.

Not supported yet: switching a `Pro_XX.narc` animation to play once.

## Epilogues (confirmed)

`2D/Epilogue1.csvb` … `Epilogue7.csvb` (one per ending; images `Epi_01` … `Epi_15`, all static `RCSN`, no animation) use the same reader/writer and round-trip byte-for-byte, as does `Prologue.csvb`. Two differences from the prologue:

1. The lone `0x06` after `ScriptStart` and the `0x05/0x09/0x06` scene-change groups name **two different** images (`Epi_02`, `Epilogue_02`). The reader reads a `0x05`/`0x06` slot as a `ProloguePointerImageReference` whenever both halves point at the start of a string in the text table (in the prologue both halves are equal, so nothing changes there).
2. The last group (`0x05/0x07/0x09`) has **no slot**: the file ends before it. The reader marks it `HasSlot = false` and the writer writes no slot for it, so the file still ends there.

| File | Ending | Texts (lines) | Image values (frames) |
|---|---|---|---|
| Epilogue1 | good (Setsuna protects CB's ideals) | 7, 3, 3 | 600, 480, 480 |
| Epilogue2 | good, uncertain | 6, 5 | 480, 480 |
| Epilogue3 / 4 / 5 | Union / AEU / HRL rules the world | 3, 4 (3) | 480, 480 |
| Epilogue6 / 7 | bad (global terrorism / CB stopped) | 7 (6), 2 (3) | 600, 480 |

Text layout and typing are the same as in the prologue (42 bytes per line, ~4 frames per character). Tested in DeSmuME by putting the translated `Epilogue1.csvb` in place of `2D/Prologue.csvb`, so NEW GAME plays it: all lines visible and centered, the game continues normally afterwards. The English texts need the editor's "Use suggested" durations (e.g. text 1: 600 → 1040 frames).

## Open questions

- `Text.hi`: is it the vertical position? Should it be recalculated when the line count changes?
- Meaning of `0x00`/`0x01` `lo` (0 or 2) and of `0x03`, `0x0B`, `0x0C`.
- Can typing speed be changed (would avoid longer durations)?
- What `LogueDefine.csvb` / `LogueCommon.narc` define.