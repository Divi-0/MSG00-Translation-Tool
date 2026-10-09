using MSG00.Translation.Application.Features.Csvb.Table;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Tests.Features.Csvb.Table;

public class ReaderTests
{
    [Fact]
    public async Task Read_ReadsTableDescriptorsAndColumnTypes()
    {
        var table = await TableFileBuilder.ReadAsync(TableFileBuilder.Build());

        table.Type.ShouldBe(CsvbFileType.OG_TABLE);
        table.Tables.Select(x => x.Name).ShouldBe(new[] { "OG_MAP", "OG_MISSION" });
        table.Tables[0].Rows.ShouldBe(3);
        table.Tables[0].ColumnTypes.ShouldBe(new[] { CsvbTable.TextColumnType, CsvbTable.IntColumnType });
        table.Tables[1].ColumnTypes.ShouldBe(new[] { CsvbTable.IdColumnType, CsvbTable.TextColumnType, CsvbTable.TextColumnType });
    }

    [Fact]
    public async Task Read_ReadsTextColumnsOnly_AndSkipsEmptyCells()
    {
        var table = await TableFileBuilder.ReadAsync(TableFileBuilder.Build());

        table.Texts.Select(x => $"{x.Table} {x.Row}/{x.Column}: {x.Text}").ShouldBe(new[]
        {
            "OG_MAP 0/0: マーシャル諸島某島",
            "OG_MAP 1/0: 宇宙",
            "OG_MISSION 0/1: 新型ＭＳ披露襲撃",
            "OG_MISSION 1/1: 新型ＭＳ披露襲撃",
            "OG_MISSION 0/2: ＡＥＵが新型ＭＳ発表会を行う",
        });
        table.Texts.ShouldAllBe(x => !x.IsChanged);
    }

    [Fact]
    public async Task Read_BlockFile_ReadsTheBlockTableAndItsTexts()
    {
        var table = await TableFileBuilder.ReadAsync(TableFileBuilder.BuildBlock());

        table.Type.ShouldBe(CsvbFileType.BLOCK);
        table.Tables.Select(x => (x.Name, x.Rows, x.ColumnTypes.Count)).ShouldBe(new[] { ("BLOCK1", 2, 5) });
        table.Texts.Where(x => x.Column == 1).Select(x => x.Text).ShouldBe(new[] { TableFileBuilder.BlockPage, TableFileBuilder.BlockCaption });
        table.Texts.ShouldNotContain(x => x.Column == 0);
        TableCells.ReadText(table, table.Tables[0], 1, 2).ShouldBe("FG_01_01");
    }

    [Fact]
    public async Task ReadText_ReadsIdColumnAsRowLabel()
    {
        var table = await TableFileBuilder.ReadAsync(TableFileBuilder.Build());

        TableCells.ReadText(table, table.Tables[1], 1, 0).ShouldBe("EVM_AEU002");
    }
}