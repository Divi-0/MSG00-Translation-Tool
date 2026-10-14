# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A translation tool for the Japanese Nintendo DS ROM of *Mobile Suit Gundam 00*. It works on the binary `.csvb` text files extracted from the ROM: it reads a `.csvb` file into a domain model, lets the user edit text in an Avalonia UI (or export/import the text as JSON), and writes a byte-compatible `.csvb` back out. All game text is **Shift-JIS** encoded (`Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` is called in `AddApplicationServices`).

## Commands

.NET 10 solution (`MSG00.Translation.Tool.sln`).

```powershell
dotnet build MSG00.Translation.Tool.sln
dotnet run --project MSG00.Translation.UI.Desktop     # desktop app

dotnet test --project tests/MSG00.Translation.Application.Tests                                      # all tests
dotnet test --project tests/MSG00.Translation.Application.Tests --filter-method "*Conversation.WriterTests*"  # subset
```

Tests: `tests/MSG00.Translation.Application.Tests` is **unit tests only** and mirrors the Application project's folder structure (`Features/Csvb/Mapi/Conversation/WriterTests.cs` tests `Features/Csvb/Mapi/Conversation/Writer.cs`). xUnit v3 (Microsoft Testing Platform, opted in via `global.json`), Shouldly for assertions, NSubstitute for mocks. Csvb tests use synthetic files from `Features/Csvb/Mapi/MapiFileBuilder.cs` and the real reader/writer via `CsvbRoundTrip.cs` (no game data in the repo). The Application project exposes internals to the test project (`InternalsVisibleTo`).

## Architecture

Projects (dependency order): `Application` ← `UI` ← `UI.Desktop`. (There is no separate Domain or Infrastructure project anymore.)

- **Application** – vertical feature slices under `Features/`; each slice holds its model, reader, writer and `ServiceExtension` (DI registration), and the namespace matches the folder (`MSG00.Translation.Application.Features.<Slice>`). Mediator (source-generated, `Mediator` 3.x — handlers return `ValueTask`).
  - `Csvb/` – shared csvb basics: `CsvbFile`, `CsvbHeader`, `CsvbFileType`, `CsvbTextLine`, `NotifyBase`, `SlotTextWriter`; `Csvb/Read` (`ICsvbReader`/`CsvbReader`, base class `CsvbMapiReader`, `ReadCsvbFile` handler); `Csvb/Write` (base class `CsvbWriter`, `AddTrailingCsvbFileZeroes`).
  - `Csvb/Mapi/` (`MapiCsvb`, `MapiHeader`, `MapiType`, `MapiService`) with `Conversation/`, `PrologueEpilogue/`, `StaffRoll/`; `Csvb/EvmBase/` (model, `Read/EvmBaseReader`, `Write/`); `Csvb/CutIn/`, `Csvb/Table/`; `Graphics/`; `Font/`; `Arm9/`; `Rom/`.
  - All game text is encoded with `Font/GameEncoding.Instance` (Shift-JIS plus the ROM's extra font characters), never with `Encoding.GetEncoding("Shift-JIS")` directly.
  - `AddApplicationServices()` registers every slice and the Shift-JIS code page provider.
- **UI** – Avalonia 12 + CommunityToolkit.Mvvm/ReactiveUI. `App.axaml.cs` builds a Generic Host and registers `AddApplicationServices()`, the `IStorageProvider` and `CsvbSelectViewModel` (which creates the editors for the selected file). `App.Services` is a static service locator used by view models.

### ROM handling

`Application/Features/Rom/RomService.cs` (`IRomService`, SceneGate.Ekona + Yarhl) opens the `.nds` into memory, lists all `.csvb`/`.fcmd`/`.narc` files grouped by `CsvbCategories` (path rules; files without any text — map/unit/effect scripts, definition tables, debug and empty test scripts — are left out via `CsvbCategories.HasText`), hands out file copies and keeps replaced files in memory. `SaveAsAsync` rebuilds a **new** ROM from the original bytes plus replacements; the opened ROM is never written. `NitroRom2BinaryParams.DecompressedProgram = true` is required: this game's ARM9 is not compressed, and without it Ekona marks it as compressed and the ROM does not boot.

`CsvbSelectViewModel` is the ROM browser (Open ROM / Save ROM as / category tree). Before opening an editor it checks that the csvb is written back byte-identical (conversations: or equivalent per `Conversation/Equivalence.cs`, for files whose only difference is the text table layout, e.g. edited by the old tool); files that fail are refused so they cannot be damaged. Editors "Apply to ROM" via `IRomService.ReplaceFile`. Every editor has JSON Export/Import through `UI/Services/JsonTextFile.cs`: Prologue and Conversation apply imported texts in order; Program texts match by ARM9 offset, table texts by table/row/column.

### Read pipeline (new structure)

1. `CsvbSelectViewModel` gets a file stream from `IRomService.OpenFile` and sends `ReadCsvbFile(stream)` via `IMediator`.
2. `ReadCsvbFileHandler` uses `ICsvbReader` to read the generic `CsvbHeader` (four int32 section offsets at `0x0C..0x1B`) and the file type from the 8 bytes at `0x20` (`MAPI\0\0\0\0`, `BLOCK1\0\0`, or EVM_BASE) → `CsvbFile`.
3. It dispatches by `CsvbFileType`:
   - `MAPI` → `IMapiService.GetMapiCsvbAsync`, which reads the 48-byte MAPI header at `0x20` and switches on `MapiHeader.MapiType` (int32 at header bytes 44..48: `PrologueEpilogue=0x0A`, `StaffRoll=0x0D`, `Conversation=0x10`), each with a reader/writer under `Features/Csvb/Mapi/<Type>` (staff roll: texts of pointer type 0x08, written with `SlotTextWriter`, `StaffRollTextCsvb`; epilogues share the prologue type and reader/writer: their 0x05/0x06 slots name two images (`ProloguePointerImageReference`) and their last group has no slot (`HasSlot = false`)).
   - `EVM_BASE` → `ReadEvmBase` Mediator request → `IEvmBaseReader` (work in progress).
   - `BLOCK` (2D/ Gunpla Navi files, one `BLOCK1` table) and `OG_TABLE` → `Features/Csvb/Table` reader (`TableCsvb`); translatable columns are listed in `TextColumns`.
4. `CsvbSelectViewModel` switches on the result type and creates the matching view model (e.g. `PrologueCsvbViewModel`).

### Reference implementation: Prologue/Epilogue

**`PrologueCsvb` is the current, working csvb implementation** — use it as the template when migrating or adding other file types. The pieces:

- Model: `Application/Features/Csvb/Mapi/PrologueEpilogue/` — `PrologueCsvb : MapiCsvb : CsvbFile`, holding `ProEpiloguePointers` (tree of `ProloguePointer`, with `ProloguePointerText` / `ProloguePointerTextObjectReference` subclasses) and `AfterTextSectionBytes` (opaque bytes preserved verbatim).
- Reader/Writer: `Application/Features/Csvb/Mapi/PrologueEpilogue/Reader.cs` and `Writer.cs` (`IReader`/`IWriter`, registered via `AddMapiPrologueEpilogue()` → `AddMapi()` → `AddApplicationServices()`).
- UI: `PrologueCsvbViewModel` / `PrologueCsvbView` (save calls `IWriter.WriteFileAsync`; JSON export/import of text pointers).

The old Domain/Infrastructure projects (legacy per-type reader/writer/service trios) have been removed; new file types get their own slice under `Features/Csvb/` with the same pieces (model, reader, writer, `ServiceExtension`, tests in the mirrored test folder).

### csvb file layout (as used by the Prologue reader/writer)

```
0x00  "CSVBv4.3" + 01 00 00 00
0x0C  int32 OffsetToEndOfPointerTable
0x10  int32 OffsetToEndOfFullHeader   (= start of text table)
0x14  int32 OffsetToEndOfTextTable
0x18  int32 OffsetToEndOfExtraTable   (= start of offset table)
0x20  48-byte MAPI header (type id at bytes 44..48)
0x50  pointer table: 8-byte entries {int32 offset into offset table, int32 pointer type}, terminated by 02 00 00 00 02 00 00 00
...   02 00 00 00 02 00 00 00 (end of full header)
...   text table: leading 4 zero bytes, then NUL-terminated Shift-JIS strings padded to 4-byte alignment
...   extra section (copied verbatim)
...   offset table: 4- or 8-byte values per pointer; text offsets are relative to OffsetToEndOfFullHeader
```

Key rules when writing:
- Consecutive pointer-table entries with the **same offset value** are grouped as `SubPointer`s of the first entry; they share one offset-table slot.
- Offset width depends on pointer type (`Image`, `StartPointer`, `Unknown3` → 4 bytes; others → 8 bytes). Keep reader and writer switch statements in sync.
- Text-object-reference strings are deduplicated in the text table; offsets are recomputed (`CalculateOffsets`) before writing, then the four CSVB header offsets are patched at the end (`OverrideCsvbHeaderAsync`).
- Every string is padded with `AddTrailingCsvbFileZeroes()` (always adds 1–4 NULs to reach 4-byte alignment).

Prologue script semantics (which pointer types mean what, timing, animation play modes, line width limits) are documented in `docs/prologue/`; conversations (MAPI 0x10, `Features/Csvb/Mapi/Conversation`; `C_error.csvb` has MAPI type 0 and is read as a conversation) in `docs/conversation/`; staff rolls (MAPI 0x0D, two text columns per page aligned with full-width spaces) in `docs/staffroll/`; texts inside the ARM9 program (in-game menu, `Features/Arm9`, edited in place, "Program texts" in the ROM tree) in `docs/arm9/`; table csvb files (`OG_*`, field and mission names in App/OG_MapMission.csvb, `Features/Csvb/Table`, texts written in place or appended, never moved) in `docs/table/`; mission scripts (`Ev/**/EVM_*.csvb`, messages = EV_SEQ command 0x67, labels 0x03 must not change, `Features/Csvb/EvmBase` with `WriteEvmBase`) in `docs/evm/`; mission cut-ins (`Cut/CUTIN.fcmd`, a csvb detected as `CUT_IN`, line command 0x34, `Features/Csvb/CutIn`, shares `SlotTextWriter` with the EVM writer) in `docs/cutin/`; graphics (2D/*.narc, "Graphics" in the ROM tree, `Features/Graphics`: NARC backgrounds (NCLR/NCGR/NSCR) and sprite cells (NCER) exported/imported as PNG with the original palette, written in place; sprite import writes only changed pixels into the front-most OAM) in `docs/graphics/`; the game font and extra characters (ä, é, ñ … in place of half-width katakana 0xA1–0xDF, presets per language, table kept in the spare bits of the glyph cells of all 11 `LC_font_12.NFTR` copies, `Features/Font`, "Font" in the ROM tree) in `docs/font/`. Header diagrams are in `docs/mapi/` (`csvb-header.png`, `mapi-header.png`); EVM reading notes are in `docs/evm/read-evm.puml`.