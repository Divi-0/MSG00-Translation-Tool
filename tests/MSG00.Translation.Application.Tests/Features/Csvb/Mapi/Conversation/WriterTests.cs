using MSG00.Translation.Application.Features.Csvb.Mapi.Conversation;

namespace MSG00.Translation.Application.Tests.Features.Csvb.Mapi.Conversation;

public class WriterTests
{
    private const int MapiPointerCountOffset = 0x34;

    [Fact]
    public async Task Write_UnchangedConversation_IsByteIdentical()
    {
        var original = MapiFileBuilder.ConversationWithChoice().Build();

        var written = await CsvbRoundTrip.WriteAsync(await CsvbRoundTrip.ReadConversationAsync(original));

        written.ShouldBe(original);
    }

    [Fact]
    public async Task Write_LongerText_MovesFollowingTextOffsets()
    {
        var conversation = await CsvbRoundTrip.ReadConversationAsync(MapiFileBuilder.ConversationWithChoice().Build());
        var first = Texts(conversation)[0];
        first.TextLines = "I am Sumeragi Lee Noriega, the tactical\nforecaster of Celestial Being.\nNice to meet you all.";

        var reread = await CsvbRoundTrip.ReadConversationAsync(await CsvbRoundTrip.WriteAsync(conversation));

        Texts(reread).Select(x => x.TextLines).ShouldBe(new[]
        {
            first.TextLines,
            "説明を受けますか？\n（ＹＥＳ／ＮＯ）",
            "YES branch text",
            "NO branch text",
        });
    }

    [Fact]
    public async Task Write_InsertedTextBox_IsReadBackAfterTheOriginalBox()
    {
        var conversation = await CsvbRoundTrip.ReadConversationAsync(MapiFileBuilder.ConversationWithChoice().Build());

        conversation.InsertTextAfter(Texts(conversation)[0], "line 4 in a new box");
        var reread = await CsvbRoundTrip.ReadConversationAsync(await CsvbRoundTrip.WriteAsync(conversation));

        reread.Pointers.Count.ShouldBe(9);
        var inserted = reread.Pointers[2].ShouldBeOfType<ConversationPointerText>();
        inserted.Type.ShouldBe(ConversationPointerType.Text);
        inserted.TextLines.ShouldBe("line 4 in a new box");
        reread.Pointers[3].Type.ShouldBe(ConversationPointerType.Question);
    }

    [Fact]
    public async Task Write_InsertedTextBox_UpdatesPointerCountInMapiHeader()
    {
        var conversation = await CsvbRoundTrip.ReadConversationAsync(MapiFileBuilder.ConversationWithChoice().Build());

        conversation.InsertTextAfter(Texts(conversation)[0], "new");
        var written = await CsvbRoundTrip.WriteAsync(conversation);

        var entries = conversation.Pointers.Sum(x => x.Group.Count());
        BitConverter.ToInt32(written, MapiPointerCountOffset).ShouldBe(entries + 1);
        (await CsvbRoundTrip.WriteAsync(await CsvbRoundTrip.ReadConversationAsync(written))).ShouldBe(written);
    }

    [Fact]
    public async Task Write_RemovedTextBox_IsGone()
    {
        var conversation = await CsvbRoundTrip.ReadConversationAsync(MapiFileBuilder.ConversationWithChoice().Build());

        conversation.Remove(Texts(conversation).Single(x => x.TextLines == "YES branch text"));
        var reread = await CsvbRoundTrip.ReadConversationAsync(await CsvbRoundTrip.WriteAsync(conversation));

        Texts(reread).ShouldNotContain(x => x.TextLines == "YES branch text");
        reread.Pointers.Count.ShouldBe(7);
    }

    private static List<ConversationPointerText> Texts(ConversationCsvb conversation) =>
        conversation.Pointers.SelectMany(x => x.Group).OfType<ConversationPointerText>().ToList();
}