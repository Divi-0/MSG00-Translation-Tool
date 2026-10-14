using Microsoft.Extensions.Logging.Abstractions;
using MSG00.Translation.Application.Features.Font;
using MSG00.Translation.Application.Features.Rom;
using NSubstitute;

namespace MSG00.Translation.Application.Tests.Features.Font;

public class FontServiceTests
{
    private readonly Dictionary<string, byte[]> _files = new()
    {
        ["2D/Common.narc"] = NitroFontBuilder.BuildNarc(),
        ["2D/PAUSE.narc"] = NitroFontBuilder.BuildNarc(),
        ["2D/Other.narc"] = NitroFontBuilder.BuildNarcWithoutFont(),
        ["2D/Prologue.csvb"] = [1, 2, 3],
    };

    private readonly IRomService _rom = Substitute.For<IRomService>();
    private readonly GameEncoding _encoding = new();
    private readonly FontService _sut;

    public FontServiceTests()
    {
        var list = _files.Keys.Select(x => new RomFile(x, x.Split('/')[^1], "Graphics", 0)).ToList();
        _rom.CsvbFiles.Returns(list);
        _rom.OpenFile(Arg.Any<string>()).Returns(x => new MemoryStream(_files[x.Arg<string>()], writable: false));
        _rom.When(x => x.ReplaceFile(Arg.Any<string>(), Arg.Any<byte[]>())).Do(x => _files[x.ArgAt<string>(0)] = x.ArgAt<byte[]>(1));
        _sut = new FontService(_rom, _encoding, NullLogger<FontService>.Instance);
    }

    private FontCharacter Character(byte slot, char character) => new(slot, character, _sut.CreateGlyph(character) ?? new Glyph(6, 11));

    [Fact]
    public void FontFiles_AreTheArchivesWithAFont_MainFontFirst()
    {
        _sut.FontFiles.ShouldBe(["2D/Common.narc", "2D/PAUSE.narc"]);
    }

    [Fact]
    public void OriginalFont_HasNoExtraCharacters()
    {
        _sut.LoadCharacterTable().IsEmpty.ShouldBeTrue();
        _sut.ReadCharacters().ShouldBeEmpty();
    }

    [Fact]
    public void Apply_WritesEveryFontCopy_InPlace()
    {
        var sizes = _files.ToDictionary(x => x.Key, x => x.Value.Length);

        _sut.Apply([Character(0xA1, 'ä'), Character(0xA2, 'ß')]);

        foreach (var path in _sut.FontFiles)
        {
            _files[path].Length.ShouldBe(sizes[path]);
            var font = FontService.FindFonts(_files[path]).Single();
            font.ReadGlyph(0xA2).SamePixels(GlyphFactory.Create('ß', _ => null)!).ShouldBeTrue();
            CharacterTable.FromBytes(font.ReadSpareBytes).Characters.ShouldBe(new Dictionary<byte, char> { [0xA1] = 'ä', [0xA2] = 'ß' });
        }

        _rom.DidNotReceive().ReplaceFile("2D/Other.narc", Arg.Any<byte[]>());
    }

    [Fact]
    public void Apply_SetsTheEncoding()
    {
        _sut.Apply([Character(0xA1, 'ä')]);

        _encoding.GetBytes("ä").ShouldBe([0xA1]);
    }

    [Fact]
    public void Applied_Characters_AreReadBack()
    {
        var applied = new[] { Character(0xA1, 'ä'), Character(0xB0, 'ç') };
        _sut.Apply(applied);
        _encoding.Characters = CharacterTable.Empty;

        _sut.LoadCharacterTable().Characters.Count.ShouldBe(2);
        _encoding.GetString([0xB0]).ShouldBe("ç");
        var read = _sut.ReadCharacters();
        read.Select(x => (x.Slot, x.Character)).ShouldBe([((byte)0xA1, 'ä'), ((byte)0xB0, 'ç')]);
        read[1].Glyph.SamePixels(applied[1].Glyph).ShouldBeTrue();
    }

    [Fact]
    public void Apply_Nothing_ClearsTheTable_KeepsGlyphs()
    {
        _sut.Apply([Character(0xA1, 'ä')]);
        var glyph = _sut.ReadCharacters().Single().Glyph;

        _sut.Apply([]);

        _sut.LoadCharacterTable().IsEmpty.ShouldBeTrue();
        FontService.FindFonts(_files["2D/Common.narc"]).Single().ReadGlyph(0xA1).SamePixels(glyph).ShouldBeTrue();
    }

    [Fact]
    public void Render_DrawsExtraCharactersBeforeTheyAreApplied()
    {
        var image = _sut.Render("aä\nあ", [Character(0xA1, 'ä')]);

        image.Width.ShouldBe(12);
        image.Height.ShouldBe(22);
        // the diaeresis of ä (row 1, column 6 + 2) is black, the same pixel above a is white
        image.Pixels[(1 * 12 + 8) * 4].ShouldBe((byte)0);
        image.Pixels[(1 * 12 + 2) * 4].ShouldBe((byte)255);
        _sut.LoadCharacterTable().IsEmpty.ShouldBeTrue();
    }
}