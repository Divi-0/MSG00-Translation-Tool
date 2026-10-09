using MSG00.Translation.Application.Features.Csvb.Mapi.Conversation;

namespace MSG00.Translation.Application.Tests.Features.Csvb.Mapi.Conversation;

public class EquivalenceTests
{
    [Fact]
    public async Task OldToolPadding_IsNotByteIdentical_ButEquivalent()
    {
        // the old tool kept every text at its original position and filled the rest with zeros
        var original = MapiFileBuilder.ConversationWithChoice(extraZeros: 12).Build();
        var (fromOriginal, written, fromWritten) = await RoundTripAsync(original);

        written.ShouldNotBe(original);
        Equivalence.IsEquivalent(original, fromOriginal, written, fromWritten).ShouldBeTrue();
    }

    [Fact]
    public async Task UnchangedFile_IsEquivalent()
    {
        var original = MapiFileBuilder.ConversationWithChoice().Build();
        var (fromOriginal, written, fromWritten) = await RoundTripAsync(original);

        Equivalence.IsEquivalent(original, fromOriginal, written, fromWritten).ShouldBeTrue();
    }

    [Fact]
    public async Task ChangedText_IsNotEquivalent()
    {
        var original = MapiFileBuilder.ConversationWithChoice().Build();
        var (fromOriginal, written, _) = await RoundTripAsync(original);

        var changed = await CsvbRoundTrip.ReadConversationAsync(written);
        changed.Pointers.SelectMany(x => x.Group).OfType<ConversationPointerText>().First().TextLines = "changed";

        Equivalence.IsEquivalent(original, fromOriginal, written, changed).ShouldBeFalse();
    }

    [Fact]
    public async Task ChangedCharacterSlot_IsNotEquivalent()
    {
        var original = MapiFileBuilder.ConversationWithChoice().Build();
        var (fromOriginal, written, _) = await RoundTripAsync(original);

        var changed = await CsvbRoundTrip.ReadConversationAsync(written);
        changed.Pointers[0].Slot[0] = 3; // other character

        Equivalence.IsEquivalent(original, fromOriginal, written, changed).ShouldBeFalse();
    }

    [Fact]
    public async Task MissingEndOfPointerTableMarker_IsNotEquivalent()
    {
        var original = MapiFileBuilder.ConversationWithChoice().Build();
        var endOfPointerTable = BitConverter.ToInt32(original, 0x0C);
        original[endOfPointerTable] = 3;
        var (fromOriginal, written, fromWritten) = await RoundTripAsync(original);

        Equivalence.IsEquivalent(original, fromOriginal, written, fromWritten).ShouldBeFalse();
    }

    private static async Task<(ConversationCsvb FromOriginal, byte[] Written, ConversationCsvb FromWritten)> RoundTripAsync(byte[] original)
    {
        var fromOriginal = await CsvbRoundTrip.ReadConversationAsync(original);
        var written = await CsvbRoundTrip.WriteAsync(fromOriginal);
        return (fromOriginal, written, await CsvbRoundTrip.ReadConversationAsync(written));
    }
}