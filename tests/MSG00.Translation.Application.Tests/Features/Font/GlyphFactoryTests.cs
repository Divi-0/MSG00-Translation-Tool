using MSG00.Translation.Application.Features.Font;

namespace MSG00.Translation.Application.Tests.Features.Font;

public class GlyphFactoryTests
{
    private static readonly NitroFont Font = new(NitroFontBuilder.Build(), 0);

    private static Glyph? FontGlyph(char c) => Font.HasCode(c) ? Font.ReadGlyph(c) : null;

    [Fact]
    public void Create_LowercaseWithAccent_KeepsLetter_AccentAboveXHeight()
    {
        var glyph = GlyphFactory.Create('ä', FontGlyph)!;

        var rows = glyph.ToRows();
        rows[0].ShouldBe("......");
        rows[1].ShouldBe("..#.#.");
        rows[2].ShouldBe("......");
        rows.Skip(3).ShouldBe(Font.ReadGlyph('a').ToRows().Skip(3));
    }

    [Fact]
    public void Create_CapitalWithAccent_IsSqueezedBelowTheAccent()
    {
        var glyph = GlyphFactory.Create('Á', FontGlyph)!;

        var rows = glyph.ToRows();
        rows[0].ShouldBe("....#.");
        rows[1].ShouldBe("...#..");
        // the 4 equal middle rows lose 2: first the longest run, then the run closer to the middle
        rows.Skip(2).ShouldBe(["...#..", "...#..", "...#..", "..#.#.", "..#.#.", ".#####", ".#...#", ".#...#", "......"]);
    }

    [Fact]
    public void Create_DiaeresisOnCapital_TakesTheTopRow()
    {
        var rows = GlyphFactory.Create('Ö', FontGlyph)!.ToRows();

        rows[0].ShouldBe("..#.#.");
        rows[1].ShouldBe("......");
        rows[2].ShouldBe(Font.ReadGlyph('O').ToRows()[0]);
    }

    [Fact]
    public void Create_AccentedI_IsDotless()
    {
        var rows = GlyphFactory.Create('í', FontGlyph)!.ToRows();

        rows[0].ShouldBe("....#.");
        rows[1].ShouldBe("...#..");
        rows[2].ShouldBe("......");
    }

    [Fact]
    public void Create_Cedilla_GoesBelow()
    {
        var rows = GlyphFactory.Create('ç', FontGlyph)!.ToRows();

        rows[10].ShouldBe("...#..");
        rows.Take(10).ShouldBe(Font.ReadGlyph('c').ToRows().Take(10));
    }

    [Fact]
    public void Create_InvertedMarks_AreRotated()
    {
        var exclamation = Font.ReadGlyph('!').ToRows();

        GlyphFactory.Create('¡', FontGlyph)!.ToRows().ShouldBe(exclamation.Reverse());
    }

    [Fact]
    public void Create_DrawnCharacter_DoesNotNeedTheFont()
    {
        GlyphFactory.Create('ß', _ => null)!.IsEmpty.ShouldBeFalse();
    }

    [Theory]
    [InlineData('ŋ')] // no decomposition
    [InlineData('ü')] // the font of the test has no u
    public void Create_Unknown_IsNull(char character)
    {
        GlyphFactory.Create(character, FontGlyph).ShouldBeNull();
    }

    [Fact]
    public void Create_EveryPresetCharacter_HasADraft()
    {
        var box = Glyph.FromRows(Enumerable.Repeat(".#####", 10).ToList(), 6, 11);

        foreach (var character in CharacterPresets.WesternEuropean.Characters)
        {
            GlyphFactory.Create(character, _ => box).ShouldNotBeNull(character.ToString());
        }
    }
}