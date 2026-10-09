using MSG00.Translation.Application.Features.Csvb.EvmBase;

namespace MSG00.Translation.Application.Tests.Features.Csvb.EvmBase.Write;

public class WriteEvmBaseHandlerTests
{
    private const int OffsetToEndOfTextTable = 0x14;
    private const int OffsetToEndOfExtraTable = 0x18;

    [Fact]
    public async Task Write_Unchanged_IsByteIdentical()
    {
        var original = EvmFileBuilder.Build();

        var written = await EvmFileBuilder.WriteAsync(await EvmFileBuilder.ReadAsync(original));

        written.ShouldBe(original);
    }

    [Fact]
    public async Task Write_ShorterText_IsWrittenInPlace()
    {
        var original = EvmFileBuilder.Build();
        var evm = await EvmFileBuilder.ReadAsync(original);
        Message(evm, EvmFileBuilder.BattleIndex).Pointer.Offset!.Text = "Target down";

        var written = await EvmFileBuilder.WriteAsync(evm);
        var reread = await EvmFileBuilder.ReadAsync(written);

        written.Length.ShouldBe(original.Length);
        Message(reread, EvmFileBuilder.BattleIndex).Pointer.Offset!.Text.ShouldBe("Target down");
    }

    [Fact]
    public async Task Write_LongerText_IsAppendedAndKeepsOtherData()
    {
        var original = EvmFileBuilder.Build();
        var evm = await EvmFileBuilder.ReadAsync(original);
        Message(evm, EvmFileBuilder.BattleIndex).Pointer.Offset!.Text = "The attack target has been destroyed.";

        var written = await EvmFileBuilder.WriteAsync(evm);
        var reread = await EvmFileBuilder.ReadAsync(written);

        var growth = written.Length - original.Length;
        growth.ShouldBe(40); // 37 characters + NUL padding
        BitConverter.ToInt32(written, OffsetToEndOfTextTable).ShouldBe(BitConverter.ToInt32(original, OffsetToEndOfTextTable) + growth);
        BitConverter.ToInt32(written, OffsetToEndOfExtraTable).ShouldBe(BitConverter.ToInt32(original, OffsetToEndOfExtraTable) + growth);
        Message(reread, EvmFileBuilder.BattleIndex).Pointer.Offset!.Text.ShouldBe("The attack target has been destroyed.");
        Message(reread, EvmFileBuilder.BoxIndex).Pointer.Offset!.Text.ShouldBe(EvmFileBuilder.Box);
        EvmMessages.OriginalText(reread, EvmMessages.LabelTextOffsets(reread).Single()).ShouldBe(EvmFileBuilder.Label);
        reread.PointerAndValue[EvmFileBuilder.OtherIndex].Offset!.Value.ShouldBe(evm.PointerAndValue[EvmFileBuilder.OtherIndex].Offset!.Value);
    }

    [Fact]
    public async Task Write_SharedTextChangedForOneMessage_KeepsTheOther()
    {
        var evm = await EvmFileBuilder.ReadAsync(EvmFileBuilder.Build());
        Message(evm, EvmFileBuilder.SharedBoxIndex).Pointer.Offset!.Text = "Short";

        var reread = await EvmFileBuilder.ReadAsync(await EvmFileBuilder.WriteAsync(evm));

        Message(reread, EvmFileBuilder.SharedBoxIndex).Pointer.Offset!.Text.ShouldBe("Short");
        Message(reread, EvmFileBuilder.BoxIndex).Pointer.Offset!.Text.ShouldBe(EvmFileBuilder.Box);
    }

    [Fact]
    public async Task Write_SharedTextChangedForAllMessages_IsWrittenInPlaceOnce()
    {
        var original = EvmFileBuilder.Build();
        var evm = await EvmFileBuilder.ReadAsync(original);
        Message(evm, EvmFileBuilder.BoxIndex).Pointer.Offset!.Text = "Gundam controls.";
        Message(evm, EvmFileBuilder.SharedBoxIndex).Pointer.Offset!.Text = "Gundam controls.";

        var written = await EvmFileBuilder.WriteAsync(evm);
        var reread = await EvmFileBuilder.ReadAsync(written);

        written.Length.ShouldBe(original.Length);
        Message(reread, EvmFileBuilder.BoxIndex).TextOffset.ShouldBe(Message(reread, EvmFileBuilder.SharedBoxIndex).TextOffset);
        Message(reread, EvmFileBuilder.SharedBoxIndex).Pointer.Offset!.Text.ShouldBe("Gundam controls.");
    }

    private static EvmMessage Message(EvmBaseCsvb evm, int index) => EvmMessages.Find(evm).Single(x => x.Index == index);
}