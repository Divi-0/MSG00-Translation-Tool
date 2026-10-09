using MSG00.Translation.Application.Features.Graphics;

namespace MSG00.Translation.Application.Tests.Features.Graphics;

public class WriterTests
{
    [Fact]
    public void Write_Unchanged_IsByteIdentical()
    {
        var narc = NarcFileBuilder.Build();

        new Writer().Write(narc, new Reader().ReadBackgrounds(narc).Single()).ShouldBe(narc);
    }

    [Fact]
    public void Write_ChangedTilesAndMap_AreReadBackInPlace()
    {
        var narc = NarcFileBuilder.Build(8);
        var background = new Reader().ReadBackgrounds(narc).Single();
        background.Tiles = Enumerable.Repeat((byte)3, background.Tiles.Length).ToArray();
        background.Map = [2, 2 | 1 << 11];

        var written = new Writer().Write(narc, background);
        var reread = new Reader().ReadBackgrounds(written).Single();

        written.Length.ShouldBe(narc.Length);
        reread.Tiles.ShouldBe(background.Tiles);
        reread.Map.ShouldBe(background.Map);
        var other = Narc.Read(narc).First();
        written.AsSpan(other.Offset, other.Length).ToArray().ShouldBe(narc.AsSpan(other.Offset, other.Length).ToArray());
    }

    [Fact]
    public void Write_TilesOfOtherSize_Throws()
    {
        var narc = NarcFileBuilder.Build();
        var background = new Reader().ReadBackgrounds(narc).Single();
        background.Tiles = new byte[background.Tiles.Length + 32];

        Should.Throw<InvalidOperationException>(() => new Writer().Write(narc, background));
    }
}