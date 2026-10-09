using MSG00.Translation.Application.Features.Graphics;

namespace MSG00.Translation.Application.Tests.Features.Graphics;

public class SpriteImageTests
{
    [Fact]
    public void ReadSprites_ReadsCellsAndOams()
    {
        var sprites = Read();

        sprites.Name.ShouldBe(NarcFileBuilder.SpriteName);
        sprites.Cells.Select(x => (x.Index, x.Oams.Count, x.Width, x.Height)).ShouldBe(new[] { (0, 3, 16, 8), (1, 1, 8, 8) });
        sprites.Cells[0].Oams.Select(x => (x.X, x.TileOffset)).ShouldBe(new[] { (0, 32), (0, 0), (8, 64) });
    }

    [Fact]
    public void Render_FirstOamIsInFront()
    {
        var sprites = Read();

        var image = SpriteImage.Render(sprites, sprites.Cells[0]);

        Pixel(image, 0, 0).ShouldBe((0, 255, 0, 255)); // green half of the front OAM
        Pixel(image, 4, 0).ShouldBe((255, 0, 0, 255)); // red of the OAM behind, where the front one is transparent
        Pixel(image, 8, 0).ShouldBe((0, 0, 255, 255));
    }

    [Fact]
    public void Import_RenderedImage_KeepsAllTiles()
    {
        var sprites = Read();

        SpriteImage.Import(sprites, sprites.Cells[0], SpriteImage.Render(sprites, sprites.Cells[0])).ShouldBe(sprites.Tiles);
    }

    [Fact]
    public void Import_WritesChangedPixelsIntoTheFrontOamOnly()
    {
        var sprites = Read();
        var image = SpriteImage.Render(sprites, sprites.Cells[0]);
        for (var x = 0; x < 8; x++)
        {
            Set(image, x, 0, (0, 0, 255, 255)); // first row of the front OAM blue
        }

        sprites.Tiles = SpriteImage.Import(sprites, sprites.Cells[0], image);

        SpriteImage.Render(sprites, sprites.Cells[0]).Pixels.ShouldBe(image.Pixels);
        var behind = SpriteImage.Render(sprites, sprites.Cells[1]);
        Pixel(behind, 0, 0).ShouldBe((255, 0, 0, 255)); // the OAM behind keeps its tiles
        Pixel(behind, 4, 0).ShouldBe((255, 0, 0, 255)); // also where it was visible through the front OAM
    }

    [Fact]
    public void Import_TransparentPixel_IsClearedInAllOams()
    {
        var sprites = Read();
        var image = SpriteImage.Render(sprites, sprites.Cells[0]);
        Set(image, 0, 0, (0, 0, 0, 0)); // green front OAM over the red one

        sprites.Tiles = SpriteImage.Import(sprites, sprites.Cells[0], image);

        Pixel(SpriteImage.Render(sprites, sprites.Cells[0]), 0, 0).A.ShouldBe(0);
        Pixel(SpriteImage.Render(sprites, sprites.Cells[1]), 0, 0).A.ShouldBe(0);
    }

    [Fact]
    public void Import_UnchangedPixels_KeepTheirTiles()
    {
        var sprites = Read();
        var image = SpriteImage.Render(sprites, sprites.Cells[0]);
        Set(image, 8, 0, (255, 0, 0, 255)); // one pixel of the blue OAM

        var tiles = SpriteImage.Import(sprites, sprites.Cells[0], image);

        tiles.Where((b, i) => b != sprites.Tiles[i]).Count().ShouldBe(1);
    }

    [Fact]
    public void Import_WrongSize_Throws()
    {
        var sprites = Read();

        Should.Throw<InvalidOperationException>(() => SpriteImage.Import(sprites, sprites.Cells[0], new RgbaImage(8, 8, new byte[256])))
            .Message.ShouldContain("16 × 8");
    }

    [Fact]
    public void Write_Sprites_WritesTilesInPlace()
    {
        var narc = NarcFileBuilder.BuildSprites();
        var sprites = new Reader().ReadSprites(narc).Single();
        sprites.Tiles = Enumerable.Repeat((byte)0x33, sprites.Tiles.Length).ToArray();

        var written = new Writer().Write(narc, sprites);

        written.Length.ShouldBe(narc.Length);
        new Reader().ReadSprites(written).Single().Tiles.ShouldBe(sprites.Tiles);
    }

    private static SpriteSet Read() => new Reader().ReadSprites(NarcFileBuilder.BuildSprites()).Single();

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