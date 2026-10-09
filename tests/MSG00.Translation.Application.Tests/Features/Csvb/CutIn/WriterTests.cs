namespace MSG00.Translation.Application.Tests.Features.Csvb.CutIn;

public class WriterTests
{
    [Fact]
    public async Task Write_Unchanged_IsByteIdentical()
    {
        var original = CutInFileBuilder.Build();

        var written = await CutInFileBuilder.WriteAsync(await CutInFileBuilder.ReadAsync(original));

        written.ShouldBe(original);
    }

    [Fact]
    public async Task Write_ShorterLine_IsWrittenInPlace()
    {
        var original = CutInFileBuilder.Build();
        var cutIn = await CutInFileBuilder.ReadAsync(original);
        cutIn.Lines[1].Text = "Destroy the target.";

        var written = await CutInFileBuilder.WriteAsync(cutIn);
        var reread = await CutInFileBuilder.ReadAsync(written);

        written.Length.ShouldBe(original.Length);
        reread.Lines[1].Text.ShouldBe("Destroy the target.");
        reread.Lines[0].Text.ShouldBe(CutInFileBuilder.ExiaLine);
    }

    [Fact]
    public async Task Write_LongerLine_IsAppendedAndKeepsOtherSlots()
    {
        var original = CutInFileBuilder.Build();
        var cutIn = await CutInFileBuilder.ReadAsync(original);
        cutIn.Lines[0].Text = "Mission complete. Exia, returning to base.";

        var written = await CutInFileBuilder.WriteAsync(cutIn);
        var reread = await CutInFileBuilder.ReadAsync(written);

        written.Length.ShouldBeGreaterThan(original.Length);
        reread.Lines[0].Text.ShouldBe("Mission complete. Exia, returning to base.");
        reread.Lines[0].DisplayTime.ShouldBe(cutIn.Lines[0].DisplayTime);
        reread.Lines[1].Text.ShouldBe(CutInFileBuilder.TargetLine);
    }
}