using MSG00.Translation.Application.Features.Csvb.Table;

namespace MSG00.Translation.Application.Tests.Features.Csvb.Table;

public class WriterTests
{
    private const int OffsetToEndOfTextTable = 0x14;
    private const int OffsetToEndOfExtraTable = 0x18;

    [Fact]
    public async Task Write_Unchanged_IsByteIdentical()
    {
        var original = TableFileBuilder.Build();

        var written = await TableFileBuilder.WriteAsync(await TableFileBuilder.ReadAsync(original));

        written.ShouldBe(original);
    }

    [Fact]
    public async Task Write_TextThatFits_IsWrittenInPlace()
    {
        var original = TableFileBuilder.Build();
        var table = await TableFileBuilder.ReadAsync(original);
        Text(table, "OG_MAP", 0, 0).Text = "Marshall Is.";

        var written = await TableFileBuilder.WriteAsync(table);
        var reread = await TableFileBuilder.ReadAsync(written);

        written.Length.ShouldBe(original.Length);
        Text(reread, "OG_MAP", 0, 0).Text.ShouldBe("Marshall Is.");
        Text(reread, "OG_MAP", 0, 0).TextOffset.ShouldBe(Text(table, "OG_MAP", 0, 0).TextOffset);
        Text(reread, "OG_MAP", 1, 0).Text.ShouldBe("宇宙");
    }

    [Fact]
    public async Task Write_LongerText_IsAppendedAndKeepsExistingData()
    {
        var original = TableFileBuilder.Build();
        var table = await TableFileBuilder.ReadAsync(original);
        Text(table, "OG_MAP", 1, 0).Text = "Space (outside of the Earth's orbit)";

        var written = await TableFileBuilder.WriteAsync(table);
        var reread = await TableFileBuilder.ReadAsync(written);

        var growth = written.Length - original.Length;
        growth.ShouldBe(36 + 4); // 36 characters + NUL padding
        BitConverter.ToInt32(written, OffsetToEndOfTextTable).ShouldBe(BitConverter.ToInt32(original, OffsetToEndOfTextTable) + growth);
        BitConverter.ToInt32(written, OffsetToEndOfExtraTable).ShouldBe(BitConverter.ToInt32(original, OffsetToEndOfExtraTable) + growth);
        written[^TableFileBuilder.DataArea.Length..].ShouldBe(TableFileBuilder.DataArea);

        Text(reread, "OG_MAP", 1, 0).Text.ShouldBe("Space (outside of the Earth's orbit)");
        reread.Texts.Where(x => !(x.Table == "OG_MAP" && x.Row == 1)).Select(x => (x.TextOffset, x.Text))
            .ShouldBe(table.Texts.Where(x => !(x.Table == "OG_MAP" && x.Row == 1)).Select(x => (x.TextOffset, x.Original)));
    }

    [Fact]
    public async Task Write_SharedText_IsAppendedForTheChangedCellOnly()
    {
        var table = await TableFileBuilder.ReadAsync(TableFileBuilder.Build());
        Text(table, "OG_MISSION", 1, 1).Text = "Short";

        var reread = await TableFileBuilder.ReadAsync(await TableFileBuilder.WriteAsync(table));

        Text(reread, "OG_MISSION", 1, 1).Text.ShouldBe("Short");
        Text(reread, "OG_MISSION", 0, 1).Text.ShouldBe("新型ＭＳ披露襲撃");
    }

    [Fact]
    public async Task Write_SeveralLongerTexts_AreAllReadBack()
    {
        var table = await TableFileBuilder.ReadAsync(TableFileBuilder.Build());
        Text(table, "OG_MAP", 0, 0).Text = "An island in the Marshall Islands";
        Text(table, "OG_MISSION", 0, 2).Text = "The AEU will hold a presentation of its new mobile suit.\nAttack it.";

        var reread = await TableFileBuilder.ReadAsync(await TableFileBuilder.WriteAsync(table));

        Text(reread, "OG_MAP", 0, 0).Text.ShouldBe("An island in the Marshall Islands");
        Text(reread, "OG_MISSION", 0, 2).Text.ShouldBe("The AEU will hold a presentation of its new mobile suit.\nAttack it.");
        Text(reread, "OG_MISSION", 0, 1).Text.ShouldBe("新型ＭＳ披露襲撃");
    }

    [Fact]
    public async Task Write_BlockFileUnchanged_IsByteIdentical()
    {
        var original = TableFileBuilder.BuildBlock();

        var written = await TableFileBuilder.WriteAsync(await TableFileBuilder.ReadAsync(original));

        written.ShouldBe(original);
    }

    [Fact]
    public async Task Write_BlockFileLongerCaption_IsAppendedAndKeepsImageNames()
    {
        var original = TableFileBuilder.BuildBlock();
        var table = await TableFileBuilder.ReadAsync(original);
        Text(table, "BLOCK1", 1, 1).Text = "FRONT VIEW (front of the Gunpla model)";

        var written = await TableFileBuilder.WriteAsync(table);
        var reread = await TableFileBuilder.ReadAsync(written);

        written.Length.ShouldBeGreaterThan(original.Length);
        BitConverter.ToInt32(written, OffsetToEndOfExtraTable).ShouldBe(written.Length);
        Text(reread, "BLOCK1", 1, 1).Text.ShouldBe("FRONT VIEW (front of the Gunpla model)");
        Text(reread, "BLOCK1", 0, 1).Text.ShouldBe(TableFileBuilder.BlockPage);
        reread.Texts.Where(x => x.Column > 1).Select(x => x.Text).ShouldBe(new[] { "FG", "FG", "FG", "FG_01_01", "FG_01_01", "FG_01_01" }, ignoreOrder: true);
    }

    private static TableText Text(TableCsvb table, string name, int row, int column) =>
        table.Texts.Single(x => x.Table == name && x.Row == row && x.Column == column);
}