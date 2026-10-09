using MSG00.Translation.Application.Features.Csvb.Mapi.Conversation;

namespace MSG00.Translation.Application.Tests.Features.Csvb.Mapi.Conversation;

public class ReaderTests
{
    [Fact]
    public async Task Read_GroupsPointersSharingASlot()
    {
        var conversation = await CsvbRoundTrip.ReadConversationAsync(MapiFileBuilder.ConversationWithChoice().Build());

        conversation.Pointers.Select(x => string.Join(",", x.Group.Select(p => p.Type))).ShouldBe(new[]
        {
            "Character",
            "Text",
            "Question",
            "YesBranch,Character",
            "Text",
            "BranchEnd,NoBranch,Character",
            "Text",
            "BranchEnd",
        });
    }

    [Fact]
    public async Task Read_ReadsTexts()
    {
        var conversation = await CsvbRoundTrip.ReadConversationAsync(MapiFileBuilder.ConversationWithChoice().Build());

        var texts = conversation.Pointers.SelectMany(x => x.Group).OfType<ConversationPointerText>().ToList();
        texts.Select(x => x.TextLines).ShouldBe(new[] { "演習はまずまずだったようね。\n二行目", "説明を受けますか？\n（ＹＥＳ／ＮＯ）", "YES branch text", "NO branch text" });
        texts[1].Type.ShouldBe(ConversationPointerType.Question);
    }

    [Fact]
    public async Task Read_KeepsSlotsAndSections()
    {
        var conversation = await CsvbRoundTrip.ReadConversationAsync(MapiFileBuilder.ConversationWithChoice().Build());

        var character = conversation.Pointers[0];
        BitConverter.ToInt32(character.Slot, 0).ShouldBe(5);
        BitConverter.ToInt32(character.Slot, 4).ShouldBe(2);
        conversation.Pointers[^1].Slot.ShouldBeEmpty();
        conversation.OffsetTablePrefix.ShouldBe(new byte[] { 0x0A, 0, 0, 0 });
        conversation.AfterTextSectionBytes.ShouldBe(MapiFileBuilder.ExtraSection);
    }
}