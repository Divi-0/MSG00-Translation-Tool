using MSG00.Translation.Application.Features.Csvb;
using MSG00.Translation.Application.Features.Csvb.Mapi;
using MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue;

namespace MSG00.Translation.Application.Tests.Features.Csvb.Mapi.PrologueEpilogue;

public class WriterTests
{
    private const long Transition = (60L << 32) | 2;

    [Fact]
    public async Task Write_EndOfPointerTableMarker_IsWrittenOnce()
    {
        var written = await CsvbRoundTrip.WriteAsync(CreatePrologue());

        var endOfPointerTable = BitConverter.ToInt32(written, 0x0C);
        var endOfFullHeader = BitConverter.ToInt32(written, 0x10);
        (endOfFullHeader - endOfPointerTable).ShouldBe(8);
        written[endOfPointerTable..endOfFullHeader].ShouldBe(new byte[] { 2, 0, 0, 0, 2, 0, 0, 0 });
        written[endOfFullHeader..(endOfFullHeader + 4)].ShouldBe(new byte[] { 0, 0, 0, 0 });
    }

    [Fact]
    public async Task Write_ReadBack_KeepsTextsAndValues()
    {
        var written = await CsvbRoundTrip.WriteAsync(CreatePrologue());

        var reread = await CsvbRoundTrip.ReadPrologueAsync(written);

        var text = reread.ProEpiloguePointers.OfType<ProloguePointerText>().ShouldHaveSingleItem();
        text.TextLines.ShouldBe("A.D. 2307.\nAs the Earth's fossil fuels were exhausted,");
        ((int)(text.OffsetValue >> 32)).ShouldBe(54);
        ShowImage(reread).OffsetValue.ShouldBe(900);
        reread.ProEpiloguePointers.OfType<ProloguePointerImageReference>().ShouldHaveSingleItem().ImageNames.ShouldBe(new[] { "pro_01" });
    }

    [Fact]
    public async Task Write_WrittenFile_IsWrittenAgainByteIdentical()
    {
        var written = await CsvbRoundTrip.WriteAsync(CreatePrologue());

        var rewritten = await CsvbRoundTrip.WriteAsync(await CsvbRoundTrip.ReadPrologueAsync(written));

        rewritten.ShouldBe(written);
    }

    [Fact]
    public async Task Write_LongerDuration_IsReadBack()
    {
        var prologue = CreatePrologue();
        ShowImage(prologue).OffsetValue = 1340;

        var reread = await CsvbRoundTrip.ReadPrologueAsync(await CsvbRoundTrip.WriteAsync(prologue));

        ShowImage(reread).OffsetValue.ShouldBe(1340);
    }

    [Fact]
    public async Task Write_SameImageNameTwice_IsStoredOnce()
    {
        var prologue = CreatePrologue();
        prologue.ProEpiloguePointers.Add(new ProloguePointerImageReference { Type = ProloguePointerType.LoadImage, OffsetValue = 0, ImageNames = { "pro_01" } });

        var reread = await CsvbRoundTrip.ReadPrologueAsync(await CsvbRoundTrip.WriteAsync(prologue));

        var references = reread.ProEpiloguePointers.OfType<ProloguePointerImageReference>().ToList();
        references.Count.ShouldBe(2);
        references[1].OffsetValue.ShouldBe(references[0].OffsetValue);
    }

    [Fact]
    public async Task Write_EpilogueSceneChange_KeepsBothImageNames()
    {
        var epilogue = CreateEpilogue();
        epilogue.ProEpiloguePointers.OfType<ProloguePointerText>().Single().TextLines = "Through Celestial Being's actions,\nthe world was given a chance to unite.";

        var reread = await CsvbRoundTrip.ReadPrologueAsync(await CsvbRoundTrip.WriteAsync(epilogue));

        var sceneChange = reread.ProEpiloguePointers.Single(x => x.Type == ProloguePointerType.SceneChange && x.HasSlot).ShouldBeOfType<ProloguePointerImageReference>();
        sceneChange.ImageNames.ShouldBe(new[] { "Epi_02", "Epilogue_02" });
        sceneChange.SubPointer.Select(x => x.Type).ShouldBe(new[] { ProloguePointerType.ClearText, ProloguePointerType.SceneChangeEnd });
        reread.ProEpiloguePointers.Single(x => x.Type == ProloguePointerType.SceneChangeEnd).ShouldBeOfType<ProloguePointerImageReference>()
            .ImageNames.ShouldBe(new[] { "Epi_01", "Epilogue_01" });
    }

    [Fact]
    public async Task Write_EpilogueLastGroupWithoutSlot_EndsTheFileBeforeTheSlot()
    {
        var written = await CsvbRoundTrip.WriteAsync(CreateEpilogue());

        var reread = await CsvbRoundTrip.ReadPrologueAsync(written);

        var last = reread.ProEpiloguePointers[^1];
        last.HasSlot.ShouldBeFalse();
        last.SubPointer.Select(x => x.Type).ShouldBe(new[] { ProloguePointerType.SwitchImage, ProloguePointerType.ClearText });
        var lastSlot = BitConverter.ToInt32(written, BitConverter.ToInt32(written, 0x0C) - 8 * 3);
        written.Length.ShouldBe(BitConverter.ToInt32(written, 0x18) + lastSlot);
        (await CsvbRoundTrip.WriteAsync(reread)).ShouldBe(written);
    }

    private static ProloguePointer ShowImage(PrologueCsvb prologue) =>
        prologue.ProEpiloguePointers.Single(x => x.Type == ProloguePointerType.ShowImage);

    /// <summary>first scene of the prologue: load image, fade in, text, show image, fade out</summary>
    private static PrologueCsvb CreatePrologue() => new()
    {
        CsvbHeader = new CsvbHeader { OffsetToEndOfPointerTable = 0, OffsetToEndOfFullHeader = 0, OffsetToEndOfTextTable = new byte[4], OffsetToEndOfExtraTable = 0 },
        MapiHeader = new MapiHeader { FullHeaderBytes = MapiFileBuilder.MapiHeader(MapiFileBuilder.PrologueEpilogueType, 7) },
        AfterTextSectionBytes = MapiFileBuilder.ExtraSection,
        ProEpiloguePointers =
        {
            new ProloguePointer { Type = ProloguePointerType.ScriptStart, OffsetValue = 1 },
            new ProloguePointerImageReference { Type = ProloguePointerType.LoadImage, OffsetValue = 0, ImageNames = { "pro_01" } },
            new ProloguePointer { Type = ProloguePointerType.TransitionBefore, OffsetValue = Transition },
            new ProloguePointerText { Type = ProloguePointerType.Text, OffsetValue = 54L << 32, TextLines = "A.D. 2307.\nAs the Earth's fossil fuels were exhausted," },
            new ProloguePointer { Type = ProloguePointerType.ShowImage, OffsetValue = 900 },
            new ProloguePointer { Type = ProloguePointerType.TransitionAfter, OffsetValue = Transition },
        },
    };

    /// <summary>
    /// shape of Epilogue1.csvb: a lone SceneChangeEnd and a SceneChange group naming two images each,
    /// and a last group whose slot lies behind the end of the file
    /// </summary>
    private static PrologueCsvb CreateEpilogue() => new()
    {
        CsvbHeader = new CsvbHeader { OffsetToEndOfPointerTable = 0, OffsetToEndOfFullHeader = 0, OffsetToEndOfTextTable = new byte[4], OffsetToEndOfExtraTable = 0 },
        MapiHeader = new MapiHeader { FullHeaderBytes = MapiFileBuilder.MapiHeader(MapiFileBuilder.PrologueEpilogueType, 7) },
        AfterTextSectionBytes = MapiFileBuilder.ExtraSection,
        ProEpiloguePointers =
        {
            new ProloguePointer { Type = ProloguePointerType.ScriptStart, OffsetValue = 1 },
            new ProloguePointerImageReference { Type = ProloguePointerType.SceneChangeEnd, OffsetValue = 0, ImageNames = { "Epi_01", "Epilogue_01" } },
            new ProloguePointer { Type = ProloguePointerType.TransitionBefore, OffsetValue = Transition },
            new ProloguePointerText { Type = ProloguePointerType.Text, OffsetValue = 48L << 32, TextLines = "ソレスタルビーイングの活動により、" },
            new ProloguePointer { Type = ProloguePointerType.ShowImage, OffsetValue = 600 },
            new ProloguePointer { Type = ProloguePointerType.TransitionAfter, OffsetValue = Transition },
            new ProloguePointerImageReference
            {
                Type = ProloguePointerType.SceneChange, OffsetValue = 0, ImageNames = { "Epi_02", "Epilogue_02" },
                SubPointer =
                {
                    new ProloguePointer { Type = ProloguePointerType.ClearText, OffsetValue = 0 },
                    new ProloguePointerImageReference { Type = ProloguePointerType.SceneChangeEnd, OffsetValue = 0, ImageNames = { "Epi_02", "Epilogue_02" } },
                },
            },
            new ProloguePointer { Type = ProloguePointerType.TransitionAfter, OffsetValue = Transition },
            new ProloguePointer
            {
                Type = ProloguePointerType.SceneChange, OffsetValue = -1, HasSlot = false,
                SubPointer =
                {
                    new ProloguePointer { Type = ProloguePointerType.SwitchImage, OffsetValue = -1, HasSlot = false },
                    new ProloguePointer { Type = ProloguePointerType.ClearText, OffsetValue = -1, HasSlot = false },
                },
            },
        },
    };
}