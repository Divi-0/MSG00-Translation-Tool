# Staff roll (2D/StaffRoll*.csvb)

The credits after the ending. There are three files: `StaffRoll.csvb`, `StaffRoll2.csvb` and `StaffRoll3.csvb`, probably one per ending. Their bytes differ, but all three have the same 71 texts in the same order.

## Layout

The files are MAPI csvb files of type `0x0D` (see [conversation](../conversation/README.md) for the MAPI layout).

- The pointer table at `0x50` (8-byte entries `{slot offset, type}`) is a script of image commands and texts:
  - types `0x05`/`0x06`/`0x07`/`0x09` reference the images `staff_NN` / `staff_rollNN` (2D/Staff_NN.narc);
  - type `0x08` shows a text. Its slot is `{text offset, parameter}`; the parameter is constant per page (27–90, probably the display time).
- Each credit page has two texts:
  - the left column: job titles, or character names under "CAST";
  - the right column: the names.
- Both texts start at the same position. The right column is moved right with leading full-width spaces (`　`), and both columns use the same line count, so empty lines (`　`) keep the rows aligned.

## Editing (`Features/Csvb/Mapi/StaffRoll`, editor "Staff Roll")

- **Reader:** reads the type `0x08` texts (`StaffRollTextCsvb`).
- **Writer:** uses the shared `SlotTextWriter`. A text that fits is written in place; otherwise it's appended to the text table and its slot is patched. An unchanged file is written back byte-identical.
- **Editor warnings:** the editor warns when a text's line count differs from the original, or a line is longer than 42 bytes. The longest original lines are 21 full-width characters including the indentation, about the screen width.
- **JSON import:** texts are matched by index plus the Japanese original, so one file (`StaffRoll_en.json`) can be imported into all three staff rolls.

## English (test ROM, October 2026)

- Job titles use English credit terms.
- Japanese names are romanized with the given name first. Voice actors and well-known staff use their established spellings (e.g. Mamoru Miyano, Seiji Mizushima).
- Companies use their English names: BANDAI NAMCO Games, BEC, Sotsu, Sunrise, Hobby Japan.
- Readings marked "UNCERTAIN" in `claudetemp/scripts/staffroll_en.py` should be checked.
- When a romanized name would make a line too long, that text's whole column is moved left by one to three full-width spaces, so its names stay aligned.

Not tested in the emulator yet: the credits only play after finishing the game.