using MSG00.Translation.Application.Features.Csvb.EvmBase;

namespace MSG00.Translation.Application.Tests.Features.Csvb.EvmBase;

public class EvmMessagesTests
{
    [Fact]
    public async Task Find_ReturnsMessageCommandsWithKind()
    {
        var evm = await EvmFileBuilder.ReadAsync(EvmFileBuilder.Build());

        var messages = EvmMessages.Find(evm);

        messages.Select(x => (x.Index, x.Kind)).ShouldBe(new[]
        {
            (EvmFileBuilder.BoxIndex, EvmMessageKind.Box),
            (EvmFileBuilder.BattleIndex, EvmMessageKind.Battle),
            (EvmFileBuilder.SharedBoxIndex, EvmMessageKind.Box),
        });
        messages.Select(x => x.Pointer.Offset!.Text).ShouldBe(new[] { EvmFileBuilder.Box, EvmFileBuilder.Battle, EvmFileBuilder.Box });
    }

    [Fact]
    public async Task Find_BoxHasLimits_BattleMessageNot()
    {
        var messages = EvmMessages.Find(await EvmFileBuilder.ReadAsync(EvmFileBuilder.Build()));

        messages[0].MaxLines.ShouldBe(3);
        messages[0].MaxBytesPerLine.ShouldBe(38);
        messages[1].MaxLines.ShouldBeNull();
    }

    [Fact]
    public async Task LabelTextOffsets_AndOriginalText()
    {
        var evm = await EvmFileBuilder.ReadAsync(EvmFileBuilder.Build());

        var label = EvmMessages.LabelTextOffsets(evm).ShouldHaveSingleItem();

        EvmMessages.OriginalText(evm, label).ShouldBe(EvmFileBuilder.Label);
    }
}