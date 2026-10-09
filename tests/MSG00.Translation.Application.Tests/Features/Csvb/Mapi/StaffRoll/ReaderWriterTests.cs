namespace MSG00.Translation.Application.Tests.Features.Csvb.Mapi.StaffRoll;

public class ReaderWriterTests
{
    private const int ImageType = 0x06;
    private const int TextType = 0x08;

    private static byte[] Build() => new MapiFileBuilder(MapiFileBuilder.StaffRollType)
        .Text("staff_01", [ImageType])
        .Text("　ＣＡＳＴ\n\n　刹那・F・セイエイ", [TextType])
        .Text("　\n　\n　　　　宮野　真守", [TextType])
        .Build();

    [Fact]
    public async Task Read_ReadsOnlyTextEntries()
    {
        var staffRoll = await CsvbRoundTrip.ReadStaffRollAsync(Build());

        staffRoll.Texts.Select(x => x.Text).ShouldBe(new[] { "　ＣＡＳＴ\n\n　刹那・F・セイエイ", "　\n　\n　　　　宮野　真守" });
    }

    [Fact]
    public async Task Write_Unchanged_IsByteIdentical()
    {
        var original = Build();

        var written = await CsvbRoundTrip.WriteAsync(await CsvbRoundTrip.ReadStaffRollAsync(original));

        written.ShouldBe(original);
    }

    [Fact]
    public async Task Write_LongerText_IsReadBackAndKeepsTheImageName()
    {
        var staffRoll = await CsvbRoundTrip.ReadStaffRollAsync(Build());
        staffRoll.Texts[0].Text = "　CAST\n\n　Setsuna F. Seiei (Meister of Gundam Exia)";

        var reread = await CsvbRoundTrip.ReadStaffRollAsync(await CsvbRoundTrip.WriteAsync(staffRoll));

        reread.Texts.Select(x => x.Text).ShouldBe(new[] { "　CAST\n\n　Setsuna F. Seiei (Meister of Gundam Exia)", "　\n　\n　　　　宮野　真守" });
    }
}