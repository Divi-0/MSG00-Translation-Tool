using MSG00.Translation.Application.Features.Font;

namespace MSG00.Translation.Application.Tests.Features.Font;

public class NitroFontTests
{
    [Fact]
    public void Read_FontInfo()
    {
        var font = new NitroFont(NitroFontBuilder.Build(), 0);

        font.Encoding.ShouldBe(2);
        font.CellWidth.ShouldBe(11);
        font.CellHeight.ShouldBe(11);
        font.SpareBitsPerGlyph.ShouldBe(7);
        font.GlyphCount.ShouldBe(NitroFontBuilder.Ascii.Count + 63 + 3);
    }

    [Fact]
    public void Read_AllCharacterMapTypes()
    {
        var font = new NitroFont(NitroFontBuilder.Build(), 0);

        font.HasCode('A').ShouldBeTrue();
        font.HasCode('B').ShouldBeFalse();
        font.HasCode(0xA1).ShouldBeTrue();
        font.HasCode(0xDF).ShouldBeTrue();
        font.HasCode(0x82A1).ShouldBeTrue();
        font.AdvanceOf(0x82A0).ShouldBe(12);
    }

    [Fact]
    public void ReadGlyph_PlacesBitmapAtLeftOffset()
    {
        var font = new NitroFont(NitroFontBuilder.Build(), 0);

        var glyph = font.ReadGlyph('i');

        glyph.Width.ShouldBe(6);
        glyph.Height.ShouldBe(11);
        glyph.ToRows().ShouldBe(["......", "...#..", "......", "...#..", "...#..", "...#..", "...#..", "...#..", "...#..", "...#..", "......"]);
    }

    [Fact]
    public void WriteGlyph_IsReadBack_AndSetsWidths()
    {
        var data = NitroFontBuilder.Build();
        var font = new NitroFont(data, 0);
        var glyph = Glyph.FromRows(["", "..#.#", "", "..###"], 6, 11);

        font.WriteGlyph(0xA1, glyph);

        var reread = new NitroFont(data, 0);
        reread.ReadGlyph(0xA1).SamePixels(glyph).ShouldBeTrue();
        reread.AdvanceOf(0xA1).ShouldBe(6);
        reread.ReadGlyph('A').ToRows()[0].ShouldBe("...#..");
    }

    [Fact]
    public void WriteGlyph_Empty_IsEmpty()
    {
        var data = NitroFontBuilder.Build();
        var font = new NitroFont(data, 0);

        font.WriteGlyph(0xA2, new Glyph(6, 11));

        font.ReadGlyph(0xA2).IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void WriteGlyph_LargerThanCell_Throws()
    {
        var font = new NitroFont(NitroFontBuilder.Build(), 0);

        Should.Throw<ArgumentException>(() => font.WriteGlyph(0xA1, new Glyph(12, 11)));
    }

    [Fact]
    public void SpareBytes_AreReadBack_AndLeaveGlyphsAlone()
    {
        var data = NitroFontBuilder.Build();
        var font = new NitroFont(data, 0);
        var glyphsBefore = font.Codes.ToDictionary(x => x, x => font.ReadGlyph(x).ToString());
        byte[] bytes = [0x4D, 0x53, 0xFF, 0x00, 0xA5, 0x81, 0x7E];

        font.WriteSpareBytes(bytes);

        var reread = new NitroFont(data, 0);
        reread.ReadSpareBytes(bytes.Length).ShouldBe(bytes);
        reread.Codes.ToDictionary(x => x, x => reread.ReadGlyph(x).ToString()).ShouldBe(glyphsBefore);
    }

    [Fact]
    public void SpareBytes_KeptWhenGlyphIsWritten()
    {
        var data = NitroFontBuilder.Build();
        var font = new NitroFont(data, 0);
        var bytes = Enumerable.Range(0, font.SpareByteCapacity).Select(i => (byte)(i * 37)).ToArray();
        font.WriteSpareBytes(bytes);

        foreach (var code in font.Codes.Where(x => x < 0x100))
        {
            font.WriteGlyph(code, Glyph.FromRows(Enumerable.Repeat("######", 11).ToList(), 6, 11));
        }

        font.ReadSpareBytes(bytes.Length).ShouldBe(bytes);
    }

    [Fact]
    public void WriteSpareBytes_TooMany_Throws()
    {
        var font = new NitroFont(NitroFontBuilder.Build(), 0);

        Should.Throw<InvalidOperationException>(() => font.WriteSpareBytes(new byte[font.SpareByteCapacity + 1]));
    }

    [Fact]
    public void New_NotAFont_Throws()
    {
        Should.Throw<InvalidDataException>(() => new NitroFont(new byte[64], 0));
    }
}