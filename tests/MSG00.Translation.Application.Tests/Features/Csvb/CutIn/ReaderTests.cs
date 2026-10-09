using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Tests.Features.Csvb.CutIn;

public class ReaderTests
{
    [Fact]
    public async Task Read_ReadsLinesOfAllCutInTables()
    {
        var cutIn = await CutInFileBuilder.ReadAsync(CutInFileBuilder.Build());

        cutIn.Type.ShouldBe(CsvbFileType.CUT_IN);
        cutIn.Lines.Select(x => (x.Table, x.Text)).ShouldBe(new[]
        {
            ("SFS0_006", CutInFileBuilder.ExiaLine),
            ("MES_TGT01", CutInFileBuilder.TargetLine),
        });
    }

    [Fact]
    public async Task Read_ReadsDisplayTime()
    {
        var cutIn = await CutInFileBuilder.ReadAsync(CutInFileBuilder.Build());

        cutIn.Lines[0].DisplayTime.ShouldBe(655360);
        (cutIn.Lines[0].DisplayTime / 4096).ShouldBe(160);
    }
}