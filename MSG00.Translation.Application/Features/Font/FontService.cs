using Microsoft.Extensions.Logging;
using MSG00.Translation.Application.Features.Graphics;
using MSG00.Translation.Application.Features.Rom;

namespace MSG00.Translation.Application.Features.Font;

/// <summary>an extra character of the font: the byte it has in the texts and how it looks</summary>
public sealed record FontCharacter(byte Slot, char Character, Glyph Glyph);

public interface IFontService
{
    /// <summary>NARC archives of the opened ROM that hold a copy of the game font (LC_font_12.NFTR)</summary>
    IReadOnlyList<string> FontFiles { get; }

    /// <summary>reads the character table of the opened ROM and lets <see cref="GameEncoding"/> use it; called when a ROM is opened</summary>
    CharacterTable LoadCharacterTable();

    /// <summary>the extra characters of the opened ROM with their glyphs</summary>
    IReadOnlyList<FontCharacter> ReadCharacters();

    /// <summary>a first draft of the character in the style of the font, null when it has to be drawn by hand</summary>
    Glyph? CreateGlyph(char character);

    /// <summary>the text as the game draws it, black on white, one 11 px row per line</summary>
    RgbaImage Render(string text, IReadOnlyList<FontCharacter> characters);

    /// <summary>
    /// Writes the glyphs and the character table into every copy of the font (in place, no file changes its size)
    /// and lets <see cref="GameEncoding"/> use the new table. Slots without a character keep their glyph.
    /// </summary>
    void Apply(IReadOnlyList<FontCharacter> characters);
}

internal sealed class FontService : IFontService
{
    /// <summary>the copy used to read the table and glyphs; the others are kept the same by <see cref="Apply"/></summary>
    private const string MainFontFile = "2D/Common.narc";

    private readonly IRomService _romService;
    private readonly GameEncoding _encoding;
    private readonly ILogger<FontService> _logger;
    private IReadOnlyList<RomFile>? _scannedFiles;
    private IReadOnlyList<string> _fontFiles = [];

    public FontService(IRomService romService, GameEncoding encoding, ILogger<FontService> logger)
    {
        _romService = romService;
        _encoding = encoding;
        _logger = logger;
    }

    public IReadOnlyList<string> FontFiles
    {
        get
        {
            // the file list is new for every opened ROM
            if (!ReferenceEquals(_scannedFiles, _romService.CsvbFiles))
            {
                _fontFiles = _romService.CsvbFiles
                    .Where(x => x.Path.EndsWith(".narc", StringComparison.OrdinalIgnoreCase) && FindFonts(ReadFile(x.Path)).Any())
                    .Select(x => x.Path)
                    .OrderBy(x => !x.Equals(MainFontFile, StringComparison.OrdinalIgnoreCase))
                    .ThenBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                _scannedFiles = _romService.CsvbFiles;
                _logger.LogInformation("Game font found in {Files}", _fontFiles);
            }

            return _fontFiles;
        }
    }

    public CharacterTable LoadCharacterTable()
    {
        var table = MainFont() is { } font ? CharacterTable.FromBytes(font.ReadSpareBytes) : CharacterTable.Empty;
        _encoding.Characters = table;
        _logger.LogInformation("Font has {Count} extra characters: {Characters}", table.Characters.Count, new string(table.Characters.Values.ToArray()));
        return table;
    }

    public IReadOnlyList<FontCharacter> ReadCharacters()
    {
        if (MainFont() is not { } font)
        {
            return [];
        }

        return CharacterTable.FromBytes(font.ReadSpareBytes).Characters
            .OrderBy(x => x.Key)
            .Select(x => new FontCharacter(x.Key, x.Value, font.ReadGlyph(x.Key)))
            .ToList();
    }

    public Glyph? CreateGlyph(char character)
    {
        var font = MainFont();
        return GlyphFactory.Create(character, c => font != null && font.HasCode(c) ? font.ReadGlyph(c) : null);
    }

    public RgbaImage Render(string text, IReadOnlyList<FontCharacter> characters)
    {
        var font = MainFont() ?? throw new InvalidOperationException("the ROM has no game font");
        var glyphs = characters.ToDictionary(x => (int)x.Slot, x => x.Glyph);
        var encoding = new GameEncoding(new CharacterTable(characters.ToDictionary(x => x.Slot, x => x.Character)));

        var lines = text.Replace("\r", string.Empty).Split('\n').Select(line =>
        {
            var bytes = encoding.GetBytes(line);
            var codes = new List<int>();
            for (var i = 0; i < bytes.Length; i++)
            {
                codes.Add(bytes[i] is >= 0x81 and <= 0x9F or >= 0xE0 and <= 0xFC && i + 1 < bytes.Length ? bytes[i] << 8 | bytes[++i] : bytes[i]);
            }

            return codes.Select(code => glyphs.TryGetValue(code, out var glyph) ? glyph : font.HasCode(code) ? font.ReadGlyph(code) : Missing(font.CellHeight)).ToList();
        }).ToList();

        var width = Math.Max(1, lines.Max(x => x.Sum(g => g.Width)));
        var height = lines.Count * font.CellHeight;
        var pixels = Enumerable.Repeat((byte)255, width * height * 4).ToArray();
        for (var row = 0; row < lines.Count; row++)
        {
            var left = 0;
            foreach (var glyph in lines[row])
            {
                for (var y = 0; y < glyph.Height; y++)
                {
                    for (var x = 0; x < glyph.Width; x++)
                    {
                        if (glyph[x, y])
                        {
                            var p = ((row * font.CellHeight + y) * width + left + x) * 4;
                            pixels[p] = pixels[p + 1] = pixels[p + 2] = 0;
                        }
                    }
                }

                left += glyph.Width;
            }
        }

        return new RgbaImage(width, height, pixels);
    }

    public void Apply(IReadOnlyList<FontCharacter> characters)
    {
        var table = new CharacterTable(characters.ToDictionary(x => x.Slot, x => x.Character));
        var tableBytes = table.ToBytes();
        foreach (var path in FontFiles)
        {
            var original = ReadFile(path);
            var narc = original.ToArray();
            foreach (var font in FindFonts(narc))
            {
                foreach (var character in characters.Where(x => font.HasCode(x.Slot)))
                {
                    font.WriteGlyph(character.Slot, character.Glyph);
                }

                font.WriteSpareBytes(tableBytes);
            }

            if (!narc.AsSpan().SequenceEqual(original))
            {
                _romService.ReplaceFile(path, narc);
            }
        }

        _encoding.Characters = table;
        _logger.LogInformation("Applied {Count} extra characters to {Files}: {Characters}", characters.Count, FontFiles, new string(characters.Select(x => x.Character).ToArray()));
    }

    private NitroFont? MainFont() => FontFiles.Count == 0 ? null : FindFonts(ReadFile(FontFiles[0])).FirstOrDefault();

    /// <summary>Shift-JIS fonts (*.NFTR) of the archive with the half-width katakana slots</summary>
    internal static IEnumerable<NitroFont> FindFonts(byte[] narc)
    {
        if (narc.Length < 16 || !narc.AsSpan(0, 4).SequenceEqual("NARC"u8))
        {
            yield break;
        }

        foreach (var entry in Narc.Read(narc).Where(x => x.Name.EndsWith(".NFTR", StringComparison.OrdinalIgnoreCase)))
        {
            var font = new NitroFont(narc, entry.Offset);
            if (font.Encoding == 2 && font.HasCode(CharacterTable.FirstSlot))
            {
                yield return font;
            }
        }
    }

    private byte[] ReadFile(string path)
    {
        using var stream = _romService.OpenFile(path);
        var bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }

    /// <summary>a box for characters the font does not have</summary>
    private static Glyph Missing(int height)
    {
        var glyph = new Glyph(GlyphFactory.Width, height);
        for (var y = 1; y < height - 1; y++)
        {
            glyph[1, y] = glyph[5, y] = true;
        }

        for (var x = 1; x <= 5; x++)
        {
            glyph[x, 1] = glyph[x, height - 2] = true;
        }

        return glyph;
    }
}