# Conversation (MAPI type 0x10)

Findings from `2D/Conv/C*_*.csvb` (85 files) and DeSmuME tests (October 2026).
Marked **confirmed** (byte-exact or seen in the emulator) or **hypothesis**.

## In the game

The first conversation is `2D/Conv/C1_001.csvb`. It starts right after the prologue: the Earth map appears on one screen; on the other a character portrait and a 3-line text box. In C1_001 Sumeragi introduces herself and the four Meisters. **Confirmed.**

## File layout (confirmed)

Same csvb/MAPI layout as the prologue (see [prologue README](../prologue/README.md#csvb-layout-confirmed)), with:

- MAPI type at `0x4C` = `0x10`; MAPI header `0x34` = number of pointer table entries + 1.
- The offset table starts with a 4-byte value before the first slot (`0A 00 00 00` in 78 files, also `0B`, `16`, `01`); kept unchanged.
- Texts are stored in pointer order, minimally padded (Shift-JIS + 1–4 NUL to 4-byte alignment), none is shared.
- The last group of a file (`BranchEnd`) has **no slot data**; the file ends at its slot offset.

All 85 files are written back byte-identical by `Features/Csvb/Mapi/Conversation` (reader/writer): the writer keeps every slot and only rebuilds the text table and the text offsets.

Files edited by the old tool keep each text at its original (Japanese) position and fill the rest with zeros, so they are not written back byte-identical. `Equivalence.IsEquivalent` accepts them when the re-read file has the same script, texts and non-text slot data (only text positions differ). In the old translated `rom.nds` 80 conversations are byte-identical and 5 equivalent.

## Script entries

| Type | Name in code | Slot | Meaning | Status |
|---|---|---|---|---|
| `0x00` | `Character` | 8 | shows a character: character id + second value (probably expression; Sumeragi alternates 1/2) | id confirmed |
| `0x01` | `Text` | 4 | text box, text offset | confirmed |
| `0x03` | `TextWithParameters` | 16 | text offset + 3 values (e.g. `35, 2, 3`), seen once (C1_041) | hypothesis |
| `0x05` | `Question` | 4 | question text before a yes/no choice | confirmed (text content) |
| `0x09` | `YesBranch` | – | start of the "yes" branch, grouped with the next `Character`/`Text` | confirmed (structure) |
| `0x0A` | `NoBranch` | – | start of the "no" branch, grouped as `BranchEnd, NoBranch, Character` | confirmed (structure) |
| `0x0D` | `BranchEnd` | – | end of a branch; the last one in the file has an empty slot | confirmed (structure) |
| `0x0B`, `0x0C` | `Unknown0B`, `Unknown0C` | – | seen once each (C1_006) | unknown |
| `0x0E` | `Unknown0E` | – | at the start with `Character` (30 files) or `EmptyTextBox` | unknown |
| `0x10` | `EmptyTextBox` | 4 | seen twice with `0x0E` (legacy name) | unknown |

Character ids (confirmed by the introductions in C1_001): 1 Setsuna, 2 Lockon, 3 Allelujah, 4 Tieria, 5 Sumeragi.

A yes/no choice (C1_003):

```
Character 5        Text "…"
Question           "「戦術支援」の説明を受けますか？ (YES/NO)"
YesBranch + Character 5
  Text … Text      yes branch
BranchEnd + NoBranch + Character 5
  Text             no branch
BranchEnd          (empty slot, end of file)
```

## Text box (confirmed)

- 3 lines per box; a 4th line is drawn below the box and cut off by the screen edge (it does not scroll).
- 38 Shift-JIS bytes per line: 19 full-width characters (longest original lines) or 38 ASCII characters. 40 characters touch the frame, 43 run past it.
- The (A) prompt icon is drawn at the bottom right over the end of the third line, so the third line should be a few characters shorter.

## Editor

`ConversationCsvbViewModel` / `Models/ConversationRows.cs` show speakers, yes/no branches (indented) and editable texts, with warnings for lines over 38 bytes and boxes with more than 3 lines. JSON export contains speaker, kind and text; import applies texts in order.

## Open questions

- Second `Character` value: expression or position?
- `0x03` parameters, `0x0B`, `0x0C`, `0x0E`, `0x10`, the offset table prefix.
## Adding text boxes (confirmed)

A new `Text` pointer group with a 4-byte slot can be inserted anywhere; the writer recalculates the slot offsets, the text offsets and MAPI header `0x34` (entries + 1). The 76-byte section after the text table is identical in all 85 files and does not depend on the number of entries. Tested in DeSmuME: a box inserted after Setsuna's box in C1_001 is shown as an extra box with Setsuna's portrait, then the conversation continues normally.

In the editor: "Move lines 4+ to new box", "Add box after" and "Remove box" (plain text boxes only).

Note: loading an emulator save state of the original ROM into a modified ROM shows a warning and can garble text; test from boot instead.