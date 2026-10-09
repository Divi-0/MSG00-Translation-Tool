using MSG00.Translation.Application.Features.Graphics;

namespace MSG00.Translation.Application.Tests.Features.Graphics;

public class BackgroundImageTests
{
    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    public void Render_DrawsTilesWithFlips(int bitsPerPixel)
    {
        var image = BackgroundImage.Render(Read(bitsPerPixel));

        (image.Width, image.Height).ShouldBe((16, 8));
        Pixel(image, 0, 0).ShouldBe((255, 0, 0, 255));
        Pixel(image, 4, 0).ShouldBe((0, 255, 0, 255));
        Pixel(image, 8, 0).ShouldBe((0, 255, 0, 255)); // cell 1 = tile 0 mirrored
        Pixel(image, 15, 7).ShouldBe((255, 0, 0, 255));
    }

    [Theory]
    [InlineData(4)]
    [InlineData(8)]
    public void Import_RenderedImage_LooksTheSameAndSharesMirroredTiles(int bitsPerPixel)
    {
        var background = Read(bitsPerPixel);
        var image = BackgroundImage.Render(background);

        var (tiles, map) = BackgroundImage.Import(background, image);
        background.Tiles = tiles;
        background.Map = map;

        BackgroundImage.Render(background).Pixels.ShouldBe(image.Pixels);
        (map[0] & 0x3FF).ShouldBe(map[1] & 0x3FF);
        tiles.Length.ShouldBe(3 * background.TileSize);
    }

    [Fact]
    public void Import_UsesNearestPaletteColor_AndKeepsTransparency()
    {
        var background = Read(8);
        var image = Image(16, 8, (250, 10, 10, 255));
        Set(image, 1, 0, (0, 0, 0, 0));

        var (tiles, map) = BackgroundImage.Import(background, image);
        background.Tiles = tiles;
        background.Map = map;

        var result = BackgroundImage.Render(background);
        Pixel(result, 0, 0).ShouldBe((255, 0, 0, 255));
        Pixel(result, 1, 0).ShouldBe((0, 0, 0, 0));
    }

    [Fact]
    public void Import_FourBitBackground_PicksThePaletteWithTheColors()
    {
        var background = Read(4);
        var image = Image(16, 8, (255, 255, 255, 255));

        var (_, map) = BackgroundImage.Import(background, image);

        map.ShouldAllBe(x => x >> 12 == 1);
    }

    [Fact]
    public void Import_WrongSize_Throws()
    {
        Should.Throw<InvalidOperationException>(() => BackgroundImage.Import(Read(4), Image(256, 192, (0, 0, 0, 255))))
            .Message.ShouldContain("16 × 8");
    }

    [Fact]
    public void Import_MoreDifferentTilesThanRoom_Throws()
    {
        var background = new Background
        {
            Name = "dst/Small", Width = 16, Height = 8, BitsPerPixel = 8,
            Colors = NarcFileBuilder.Colors.Select(c => new Rgb(c.R, c.G, c.B)).ToList(),
            Tiles = new byte[64], Map = new ushort[2],
        };
        var image = Image(16, 8, (255, 0, 0, 255));
        for (var y = 0; y < 8; y++)
        {
            for (var x = 8; x < 16; x++)
            {
                Set(image, x, y, (0, 0, 255, 255));
            }
        }

        Should.Throw<InvalidOperationException>(() => BackgroundImage.Import(background, image)).Message.ShouldContain("2 different 8×8 tiles");
    }

    private static Background Read(int bitsPerPixel) => new Reader().ReadBackgrounds(NarcFileBuilder.Build(bitsPerPixel)).Single();

    private static RgbaImage Image(int width, int height, (byte R, byte G, byte B, byte A) color)
    {
        var image = new RgbaImage(width, height, new byte[width * height * 4]);
        for (var p = 0; p < image.Pixels.Length; p += 4)
        {
            (image.Pixels[p], image.Pixels[p + 1], image.Pixels[p + 2], image.Pixels[p + 3]) = color;
        }

        return image;
    }

    private static void Set(RgbaImage image, int x, int y, (byte R, byte G, byte B, byte A) color)
    {
        var p = (y * image.Width + x) * 4;
        (image.Pixels[p], image.Pixels[p + 1], image.Pixels[p + 2], image.Pixels[p + 3]) = color;
    }

    private static (int R, int G, int B, int A) Pixel(RgbaImage image, int x, int y)
    {
        var p = (y * image.Width + x) * 4;
        return (image.Pixels[p], image.Pixels[p + 1], image.Pixels[p + 2], image.Pixels[p + 3]);
    }
}