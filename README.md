# MSG00 Translation Tool

A desktop tool for translating the Japanese Nintendo DS game *Mobile Suit Gundam 00* (機動戦士ガンダム00, game code `YG7J`).

You open your own ROM dump, browse the game files that contain text, and edit them in an editor for each file type. You can also export and import the texts as JSON. When you are done, the tool writes a **new** `.nds` file; it never modifies the ROM you opened.

> No game data is included in this repository. You need your own dump of the game.

## Features

| Content | Files in the ROM | What can be edited |
|---|---|---|
| Prologue and epilogues | `2D/Prologue.csvb`, `2D/Epilogue1-7.csvb` | texts and display duration per scene, line-width warnings |
| Conversations | `2D/Conv/C*_*.csvb`, `C_error.csvb` | text boxes (add, split, remove), speakers |
| Mission scripts | `Ev/**/EVM_*.csvb` | in-mission messages |
| Mission cut-ins | `Cut/CUTIN.fcmd` | cut-in lines |
| Tables | `OG_*.csvb`, `BLOCK` files in `2D/` | field and mission names, Gunpla Navi / gallery texts |
| Staff roll and requirements | `2D/StaffRoll*.csvb`, `2D/requirement.csvb` | credit and requirement texts |
| Program texts | ARM9 binary | main menu, in-game menu, save/load, results, button config, tactical support |
| Graphics | `2D/*.narc` | backgrounds and sprites exported/imported as PNG with the original palette |

Every editor checks before opening that the file can be written back byte-for-byte unchanged. Files that fail this check are refused, so they cannot be damaged. All game text is Shift-JIS encoded.

## Getting started

Requirements: [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet build MSG00.Translation.Tool.sln
dotnet run --project MSG00.Translation.UI.Desktop
```

1. **Open ROM**: select your `.nds` file. The tree lists all files that contain text, grouped by category.
2. Select a file to open its editor, then edit the texts or use **Import** with a JSON file.
3. **Apply to ROM** keeps the change in memory.
4. **Save ROM as** writes a new `.nds` file containing all applied changes.

Test the result in an emulator such as DeSmuME.

### JSON export / import

Every editor can export its texts to JSON and import them again. This lets you translate outside the tool and keep translations under version control.

- Prologue, epilogue and conversation texts are applied in file order.
- Program texts are matched by their ARM9 offset.
- Table texts are matched by table, row and column.

### Text limits

Each screen has its own limits, for example 42 bytes per line in the prologue, or 3 lines of 38 bytes in conversation boxes. The editors warn when a line is too long. Details are in the docs of each file type.

## Project structure

```
MSG00.Translation.Application   file formats as vertical feature slices (Features/Csvb/..., Graphics, Arm9, Rom)
MSG00.Translation.UI            Avalonia views and view models
MSG00.Translation.UI.Desktop    desktop entry point
tests/                          unit tests (xUnit v3, Shouldly, NSubstitute), mirroring the Application folders
docs/                           reverse-engineering notes for each file type
```

Each slice under `Features/` contains the model, reader, writer and DI registration for one file type. The Prologue/Epilogue slice is the reference implementation.

```powershell
dotnet test --project tests/MSG00.Translation.Application.Tests
```

The tests use synthetic files, so no game data is needed to run them.

## Documentation

File-format findings, each marked as confirmed or hypothesis:

- [Prologue and epilogues](docs/prologue/README.md): csvb layout, script commands, timing, animations
- [Conversations](docs/conversation/README.md)
- [Mission scripts (EVM)](docs/evm/README.md)
- [Mission cut-ins](docs/cutin/README.md)
- [Tables (OG / BLOCK)](docs/table/README.md)
- [Staff roll](docs/staffroll/README.md)
- [Program texts (ARM9)](docs/arm9/README.md)
- [Graphics (NARC)](docs/graphics/README.md)
- [csvb and MAPI header diagrams](docs/mapi/)

## Libraries

[Avalonia](https://avaloniaui.net/), [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet), [ReactiveUI](https://www.reactiveui.net/), [Mediator](https://github.com/martinothamar/Mediator), [SceneGate Ekona](https://github.com/SceneGate/Ekona) and [Yarhl](https://github.com/SceneGate/yarhl) (NDS file system), [ImageSharp](https://github.com/SixLabors/ImageSharp) (PNG export/import).

## License

[GPL-3.0](LICENSE). *Mobile Suit Gundam 00* and its content belong to their respective rights holders; this project is an unofficial fan tool.

---
AI assisted coding was used to help with some of the code and documentation. The AI was prompted with the existing code and documentation, and it generated suggestions that were then reviewed and edited by a human developer.