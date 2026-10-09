# Mission scripts (Ev/**/EVM_*.csvb)

97 files (`Ev/AEU`, `Ev/HRL`, `Ev/UNI`, `Ev/ETC`, `Ev/!test`), one per mission, e.g. `Ev/ETC/EVM_ETC001.csvb` = tutorial 1 ("Tutorial 1" in OG_MISSION). Analysed October 2026.

## Layout (confirmed)

The same table format as the other csvb files (see [table](../table/README.md)):

- table `EVM_BASE` (1 row) and table `EV_SEQ` (the script): rows `{slot offset, command}`, 2 number columns each
- text table: messages, labels and ids
- data area after the extra section: the command arguments ("slots"), offsets relative to its start

The existing reader (`Infrastructure/Reader/EvmBase/EvmBaseReader.cs`) reads every row as `Pointer { Value = slot, Type = command }` with the slot bytes in `Offset.Value`.

## Texts (confirmed over all 97 files)

Every Japanese text is referenced by exactly one kind of command:

| Command | Slot | Content | Count | Translate |
|---|---|---|---|---|
| `0x67` | `{0x67, display kind, text offset}` | message | 391 | yes |
| `0x03` | `{0x03, text offset}` | label (internal name like ミサイル出現１, jump target) | 148 texts | **no** |

Display kind (word 1) of `0x67`:

| Word 1 | Count | Display | Limit |
|---|---|---|---|
| 2, 3 | 85 | explanation box at the bottom of the upper screen ("A : NEXT"), e.g. tutorials | 3 lines, 38 bytes per line (a 37-character English line fits) |
| 4 | 305 | short message during battle (攻撃目標の破壊を確認, 敵の増援を確認, …) | not measured yet |

Other values in the text table are ids (`PL_002`, `MOVE`, `AB01_001`, …); some command arguments are small numbers that happen to equal a text offset — they are not text references.

## Writing (`Features/Csvb/EvmBase/Write/WriteEvmBase`)

Like the table csvb: a changed message is written in place when it fits into the bytes of the old text and no other command uses that text with a different wording; otherwise it is appended to the text table and the message's slot word 2 points to it. The text table end (`0x14`) and the data area start (`0x18`) grow by the appended size; slots are relative to the data area, so they stay valid.

Tested: all 97 files are written back byte-identical when unchanged; all 391 messages read back correctly after changing them (in place and appended), labels unchanged. In DeSmuME the English tutorial 1 messages are shown.

## Testing in the emulator

Load game, skip the conversation (Start), in-game menu: Mission Select → field → mission → A, wait 2–3 s, A: the mission starts and the first message appears. A advances the messages.
## Scripts without messages

21 of the 97 scripts contain no message (no Japanese text at all): the 16 `Ev/!test/EVM_TEST_*` files and `EVM_HRL018`, `EVM_HRL019`, `EVM_UNI013`, `EVM_UNI014`, `EVM_UNI018`. The texts shown in these missions are cut-ins, mainly the mission goal and result lines `MES_*` in `Cut/CUTIN.fcmd` (see [cutin](../cutin/README.md)). The EVM editor opens them with a hint instead of an empty list.
