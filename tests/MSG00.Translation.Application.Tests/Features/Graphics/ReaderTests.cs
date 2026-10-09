using MSG00.Translation.Application.Features.Graphics;

namespace MSG00.Translation.Application.Tests.Features.Graphics;

public class ReaderTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    public void ReadBackgrounds_ReadsPaletteTilesAndMap(int bitsPerPixel)
    {
        var background = new Reader().ReadBackgrounds(NarcFileBuilder.Build(bitsPerPixel)).ShouldHaveSingleItem();

        background.Name.ShouldBe(NarcFileBuilder.Name);
        (background.Width, background.Height, background.BitsPerPixel).ShouldBe((16, 8, bitsPerPixel));
        background.TileCount.ShouldBe(3);
        background.Map.ShouldBe(new ushort[] { 0, 1 << 10 });
        background.Colors.Take(4).ShouldBe(new[] { new Rgb(0, 0, 0), new Rgb(255, 0, 0), new Rgb(0, 255, 0), new Rgb(0, 0, 255) });
    }

    [Fact]
    public void ReadBackgrounds_PaletteWithoutColorSection_Throws()
    {
        var narc = NarcFileBuilder.Build();
        var palette = Narc.Read(narc).Single(x => x.Name.EndsWith(".NCLR"));
        narc[palette.Offset + 16] = (byte)'X'; // TTLP → XTLP

        Should.Throw<InvalidDataException>(() => new Reader().ReadBackgrounds(narc)).Message.ShouldContain("TTLP");
    }
}