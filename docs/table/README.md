# Table csvb (App/OG_MapMission.csvb)

Field and mission texts (field select after "Mission Select" in the in-game menu). Analysed October 2026.

## Layout (confirmed)

The csvb sections are used as a small table database:

```
0x00  "CSVBv4.3" + 7A 00 00 00
0x0C  OffsetToEndOfPointerTable  = end of the rows        = start of the column types
0x10  OffsetToEndOfFullHeader    = end of column types    = start of the text table
0x14  OffsetToEndOfTextTable
0x18  OffsetToEndOfExtraTable    = start of a data area (not analysed)
0x20  table descriptors, 40 bytes each, while the name starts with "OG_":
        16 bytes name, u32 flag, u32 rows, u32 row size, u32 columns, u32 column type index, u32 start (file offset of the rows)
...   rows of all tables, back to back (4 bytes per cell)
...   column types, 4 bytes per column; a table's types start at OffsetToEndOfPointerTable + column type index
...   text table (4 zero bytes, NUL-terminated Shift-JIS, padded to 4 bytes); text cells hold offsets into it
```

App/OG_MapMission.csvb has 122 tables: OG_MAP (45 rows), OG_MISSION (81), OG_CHAPTER (32), 93 × OG_SCRIPT, 26 × OG_TSUPPORT.

Column types: `0x02` number, `0x07` text, `0x09` id text (e.g. `EVM_AEU001`), `0x0A` unknown (a number).

| Table / column | Content | Translate |
|---|---|---|
| OG_MAP 0 (`0x07`) | field name (マーシャル諸島某島, …) | yes |
| OG_MAP 3, 4 (`0x02`) | map coordinates | no |
| OG_MISSION 0 (`0x09`) | mission id (`EVM_AEU001`) | no |
| OG_MISSION 6 / 7 (`0x07`) | mission name / description | yes |
| OG_CHAPTER 10 (`0x07`) | conversation file (`C1_001`) | **no** |

Because text columns can hold file names, the tool edits only a fixed list of columns (`Features/Csvb/Table/TextColumns.cs`).

### The same structure in MAPI files

MAPI files (prologue, conversations) are a table named `MAPI` with 2 number columns: the "pointer table" rows are {offset, type}, the `02 00 00 00 02 00 00 00` "marker" are its two column types, and the "MAPI type" at `0x4C` is row 0 — which is why the MAPI header row count is entries + 1.

### BLOCK files (2D/ Gunpla Navi)

The 25 BLOCK files (`BLOCK1` at `0x20`, `CsvbFileType.BLOCK`) are table csvb files with a single table named `BLOCK1`. They are read and written by the same table reader/writer and edited in the same editor.

Layout:

- The descriptors end where the first table's rows start. The reader uses that as the end; it no longer looks for the `OG_` prefix.
- There is no extra section and no data area: the file ends with the text table.

Files and rows:

| Files | Category | Rows |
|---|---|---|
| `Gunpla_Lv1`–`Lv4` | Gunpla | Navi lessons 1–4: one page per row |
| `Gunpla_2`, `Gunpla_3` | Gunpla | joke "trial" pages, "内容未定" placeholder |
| `FG_*`, `HG_*`, `HCM_Pro_*`, `ETC_1_*` | FG / HG / HCM / ETC | kit gallery: kit description + picture captions (ＦＲＯＮＴ　ＶＩＥＷ, ＡＣＴＩＯＮ　ＰＯＳＥ, price) |
| `requirement.csvb` | Requirement | 1 column, 1 row (情勢変化予想…). Changing it did not change MISSION SELECT, so it looks unused. |

Columns (5 columns, all type `0x07` = text offset):

| Column | Content |
|---|---|
| 0 | always empty (offset 0) |
| 1 | **the text**: a page or caption |
| 2–4 | image names (`GUN_01`, `FG_01_01`, …), shown as the row label. Must not change. |

**Limits (Gunpla Navi, DeSmuME, Lesson 1):**

- 37 half-width characters per line. The 38th is cut off.
- Up to 6 lines, like the longest original page.
- Pages are typed out like conversations; A goes to the next page.
- The button hints (Ａ：進む, Ｂ：戻る, START：メニュー), the ガンプラ講座 title and the Lesson buttons are not in these files.

## Writing texts

The data area after the extra section contains values that look like text offsets and is not understood yet, so existing texts never move:

- new text fits into the bytes of the old one (and the text is not shared by another cell) → written in place
- otherwise → appended to the end of the text table, the cell points to it, `0x14`/`0x18` grow by the appended size

Unchanged files are written back byte-identical. Tested in DeSmuME: an appended field name is shown on the field select screen.

## Limits measured in the game

- Field name: about 24 characters are visible before the NEW badge.
- Mission name (MISSION SELECT, bottom screen): same list layout with NEW badge, 24 characters.
- Mission description (MISSION SELECT, top screen under "ミッション 内容"): 3 lines, a 36-character English line fits; limit 38 bytes like the conversation box.

On MISSION SELECT the labels 情勢変化予想, ユニオン / 人革連 / ＡＥＵ, 影響度, ミッション 内容, Difficulty 難易度 are **graphics**: changing the faction name table in the ARM9 (`0xB7160`, 16-byte entries ＡＥＵ/ユニオン/人革連/全て) and the only text of `2D/requirement.csvb` (a BLOCK1 table with one text column) did not change them. They are in the background `dst/Briefing2_2B_BG` of `2D/Briefing.narc` and can be edited as an image, see [graphics](../graphics/README.md).