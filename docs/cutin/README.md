# Cut-ins (Cut/CUTIN.fcmd)

Cut-ins are the short lines shown over a character portrait during a mission, for example 「エクシア、ミッションを開始する」 at mission start or 「ミッション終了。　エクシア、帰還する」 at the end. The mission goal and result messages (「目標施設を破壊してください」, 「ミッション失敗…」) are cut-ins too.

All of them are in a single file: `Cut/CUTIN.fcmd`. Despite the extension it is a csvb (`CSVBv4.3`). The first table name starts with `SFS0_`, which is how `CsvbReader` detects `CsvbFileType.CUT_IN`.

## Layout

It is a generic csvb table database (see [table](../table/README.md)):

- **Table descriptors** at `0x20`, 40 bytes each: 16-byte name, flag, rows, row size, columns, column-type index, start of rows. The descriptors end where the first table's rows start.
- **Rows**: 8 bytes `{int32 slot, int32 command}`. `slot` is an offset into the data area (`OffsetToEndOfExtraTable`).
- **Data area**: the command's words, starting at the slot offset.

One table per cut-in. The tables are grouped by character (prefix):

| Prefix | Who |
|---|---|
| `SFS0` | Setsuna / Exia |
| `LOK0` | Lockon / Dynames |
| `ALE0` | Allelujah / Kyrios |
| `TEA0` | Tieria / Virtue (two sets) |
| `GRA0` | Graham |
| `PTC0` | Patrick |
| `SAA0` | Ali al-Saachez |
| `SER0` | Sergei |
| `PRS0` | Soma Peries |
| `JOH0` / `MHE0` / `NEN0` | Johann / Michael / Nena Trinity |
| `MES_*` | Mission goals and results (no portrait) |

## Line command `0x34`

Only command `0x34` carries text. Its slot has three words:

| Word | Meaning |
|---|---|
| 0 | fade-in time (e.g. `122880` = 30 × 4096) |
| 1 | display time in 1/4096 frames (e.g. `655360` = 160 frames ≈ 2.7 s) |
| 2 | text offset, relative to `OffsetToEndOfFullHeader` |

Other commands (portrait, sound, position) are kept unchanged.

- **166 lines** in the original ROM.
- Up to **2 lines** (`\n`), about **40 bytes** each (half-width ASCII). The editor warns above that.

## Writing

`Features/Csvb/CutIn/Writer` uses the shared `SlotTextWriter`, the same writer the EVM files use:

- A translation that fits in the old string's room and is not shared with another slot is written **in place**.
- Otherwise it is **appended** to the end of the text table. The slot's text offset is patched, and `OffsetToEndOfTextTable` / `OffsetToEndOfExtraTable` grow.
- An unchanged file is written back byte-identical.

The display time is not changed yet. The English lines were readable with the original times.

## Tested

In DeSmuME with the full test ROM (October 2026), the mission start cut-in shows "Exia, commencing mission." with Setsuna's portrait.