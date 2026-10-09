# Texts in the ARM9 program

Some texts are not in csvb files but inside the game program (`system/arm9` in Ekona, 770,744 bytes, loaded uncompressed at RAM `0x02000000`).
Found by searching the ROM for the Shift-JIS bytes of the texts (October 2026).

## Main menu (confirmed)

MODE SELECT after the title. The buttons NEW GAME / LOAD GAME / GUNPLA Navi and "MODE SELECT" are graphics (already English); only the help line at the bottom is text. Each help text is stored in up to four tables; all copies have to be translated.

| ARM9 offset | Text | Room | Pointer at |
|---|---|---|---|
| `0xB3804`, `0xB3880`, `0xB3910`, `0xB3B9C` | 新しくゲームを始めます。 | 27 | `0x48044`, `0x485D0`, `0x485E8`, `0x48D64` |
| `0xB38AC`, `0xB393C` | つづきからゲームを始めます。 | 31 | `0x485D8`, `0x485F0` |
| `0xB38DC`, `0xB396C` | ガンプラの作り方を教えてくれます。 | 35 | `0x485E0`, `0x485F8` |
| `0xB3BC4` | データを読めませんでした。\n電源を切って、カードを\n差し込み直して下さい。\n (save error) | 75 | `0x48EBC` |

Tested in DeSmuME: "Start a new game." / "Continue a saved game." / "Learn how to build Gunpla." are shown when moving through the menu.

## In-game menu (confirmed)

The BRIEFING menu (after loading a game: ミッション選択 / SAVE / LOAD / ボタンコンフィグ / タイトルに戻る) and its help texts.

| ARM9 offset | Text | Room (bytes) | Pointer at |
|---|---|---|---|
| `0xB9C98` | ミッションを選択します | 23 | `0x93588` |
| `0xB9CB0` | 操作方法を設定します | 23 | `0x9358C` |
| `0xB9CC8` | タイトルに戻ります | 19 | `0x93590` |
| `0xB9CDC` | セーブします | 15 | `0x93594` |
| `0xB9CEC` | ロードします | 15 | `0x93598` |
| `0xB9CFC` | ミッション選択 | 15 | `0x9359C` |
| `0xB9D0C` | ボタンコンフィグ | 19 | `0x935A0` |
| `0xB9D20` | タイトルに戻る | 15 | `0x935A4` |
| `0xB9D30` | ＳＡＶＥ | 11 | `0x935A8` |
| `0xB9D3C` | ＬＯＡＤ | 11 | `0x935AC` |
| `0xB9D68`–`0xB9DBC` | the five help texts again (2nd table) | same | `0x93728`–`0x93738` |

- Each text is referenced by exactly one pointer (RAM address `0x02000000 + offset`) in a table.
- **Room** = bytes up to the next string minus the terminating NUL. A translation that fits is written in place and the rest is filled with zeros; no pointer changes needed. All English menu texts fit.
- The menu font shows half-width ASCII. Help texts are typed out like other texts.
- Same area, not texts: resource names like `KEY_CONFIG_A`, `LC_font_12`, `SELECT_MOBJ00` — never edit them. That is why the tool uses a fixed list (`Features/Arm9/Arm9Texts.cs`) instead of scanning.
- Not text: "EARTH MAP", "BRIEFING", "Aボタン決定" are graphics.

Tested in DeSmuME: all 15 English texts shown correctly; the ARM9 written by the tool (`RomService` + `Arm9Texts`, saved with Ekona) is byte-identical to the tested one.

## Field select (confirmed)

After "Mission Select": FIELD SELECT with the field names (from App/OG_MapMission.csvb, see [table](../table/README.md)) and the help text:

| ARM9 offset | Text | Room | Pointer at |
|---|---|---|---|
| `0xB9DD8` | 出撃先のマップを選択してください | 35 | `0x93CB8` |
| `0xB9EA4` | Ａボタン：決定　Ｂボタン：戻る (screen not confirmed) | 31 | `0x95AEC` |

## Save / Load (confirmed)

LOAD/SAVE screen (from the main menu "LOAD GAME" and the in-game menu). "LOAD", "DATA 01" and "NO DATA" are graphics.

| ARM9 offset | Text | Room |
|---|---|---|
| `0xBACC8` / `0xBACA4` | ロード/セーブする箇所を選択して下さい。 | 35 |
| `0xBAC4C`, `0xBAD4C` | `%02ddays       取得した戦術支援 %03d%` — **format string** | 39 |
| `0xBAD00` / `0xBACEC` | ロードしました。 / セーブしました。 | 19 |
| `0xBADAC` / `0xBADF8` | write / read error (3 lines) | 75 |

The slot info is a printf format: the game inserts the days at `%02d` and the percentage at `%03d`. A translation must keep both placeholders in this order (the tool refuses others). Tested: "%02d days     Tactical support %03d%" shows "01 days     Tactical support 033%".

**Question table**: 128 bytes per entry from `0xBA7BC` (pointer to the table at `0x9D35C`, entries selected by index, so no pointer per text): セーブしますか？, セーブファイルを上書きしますか？, セーブを終了しますか？, ＤＡＴＡ−０１/０２/０３をロードしますか？, ロードを終了しますか？ — 127 bytes room each.

**Dialog buttons**: three sets of ＹＥＳ/ＮＯ (and one ＯＫ) at `0xB33F0`–`0xB3424` (room 7, ＯＫ 11). Which dialog uses which set is unknown; all are translated together. Tested: "Load DATA-01?" with YES / NO.

## Pause menu (during a mission)

START during a mission opens the pause menu. It has three entries, each with a help text (stored in two tables), two questions and the button hints.

| ARM9 offset | Text |
|---|---|
| `0xB4A9C` / `0xB4AAC` / `0xB4AB8` | 勝利条件確認 / 撤退する / タイトル画面に戻る |
| `0xB4A14`, `0xB4A2C`, `0xB4A4C` (2nd table `0xB4B20`, `0xB4B38`, `0xB4B58`) | help texts (back to title: 3 lines) |
| `0xB48AC` | retreat question. Starts with a line break, and each line starts with a space; keep that layout. |
| `0xB48F0` | back-to-title question. Each line starts with a space. |
| `0xB49F4`, `0xB4AD8`, `0xB4AEC`, `0xB4B0C` | button hints (Ａ：決定　Ｂ:戻る …) |

## Mission results

Messages on the results screen after a mission are stored in a table of **128-byte entries** from `0xB5D20` (127 bytes room each):

- tactical support results and failures
- newly usable tactical supports
- the number of tactical supports
- 「新たなミッションが提示されました。」 (`0xB68A0`)

The question 「セーブしますか？」 at `0xB6964` follows them.

- Avoid `%` in these texts. They may be printf format strings; use "20 percent".
- The results table labels (score, time, …) are **graphics**, not text. They need image editing, which is not implemented yet.
- These texts have not been checked in the emulator yet, because that needs a finished mission.

## Button config and BRIEFING questions

| ARM9 offset | Text | Room |
|---|---|---|
| `0xAF77A`, `0xAF83A`, `0xAF8FA`, `0xAF9BA` | help for control types A–D (below the type buttons, up to 4 lines) | 191 |
| `0xB8E88` | note: save the setting with SAVE in the briefing menu | 99 |
| `0xB8DF8` | Back to Title question (セーブしたところから再開することに…) | 67 |
| `0xB8E3C` | tactical supports will be removed, choose MS / pilot again? | 75 |

The panels above the type buttons are graphics (`Briefing.narc` `KEY_OBJ`, see [graphics](../graphics/README.md)). That includes the awakening button row (SELECT or touch → 覚醒), which only shows in the game.

Tested in DeSmuME:
- BRIEFING → Button Config shows the English panels and help texts.
- Back to Title shows "You will continue from your last save. Return to the title?".

## Tactical support

PILOT SELECT → SUPPORT shows the list of tactical supports and a description of the selected one. Order everywhere: Setsuna & Exia, Lockon & Dynames, Allelujah & Kyrios, Tieria & Virtue, Sumeragi, Karel, Christina & Feldt, Haro, Wang Liu Mei, Marina, Aeolia, Saji & Louise.

| ARM9 offset | Text | Room |
|---|---|---|
| `0xB7218` + n × `0x20` | names in the list | 31 |
| `0xB7398`, `0xB7458` | hidden units unlocked / how to select them (Exia roll-out color, Nadleeh) | 191 |
| `0xB7518` + n × `0x100` | description of the selected support | 255 |
| `0xB8118` + n × `0x100` | 【戦術支援・発動】 message when the support is used | 255 |

The card pictures and the buttons (AVAILABLE, SET, …) are graphics (`TacticalSupport.narc`). Tested in DeSmuME: the list and the descriptions are English; a description line of about 36 characters fits.

## Pilot select stats

PILOT SELECT shows the pilot's TYPE and STRONG VS. values as text (the labels are graphics, see [graphics](../graphics/README.md)). Table of 16-byte entries (15 bytes room):

| ARM9 offset | Text | English |
|---|---|---|
| `0xB7120`–`0xB7150` | 格闘 / 射撃 / 万能 / パワー | Melee / Shooting / All-round / Power |
| `0xB7160`–`0xB7190` | ＡＥＵ / ユニオン / 人革連 / 全て | AEU / Union / HRL / All |

## Longer texts

A text longer than its room would need free space in the ARM9 (or an overlay) and the pointer changed to the new address. Not needed so far.

## Testing the menu in the emulator

1. Use a game save: DeSmuME keeps it per ROM file name in `Battery\<rom name>.dsv`; copy it to the name of the test ROM.
2. Title → A → Mode select: Down, A (LOAD GAME) → A on DATA 01 → the question defaults to NO: Left, A.
3. The first conversation starts again; **Start skips it**. The BRIEFING menu appears.

## Other texts found in the ARM9 (not translated yet)

- `タイトルに戻る` also at `0xBB328` and `0xBB370`.
- The LOAD/SAVE screen texts (`ロードする箇所を選択して下さい。`, `取得した戦術支援`, `DATA-01をロードしますか？`) are still to be located.