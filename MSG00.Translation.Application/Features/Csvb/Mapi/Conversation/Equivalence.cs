
namespace MSG00.Translation.Application.Features.Csvb.Mapi.Conversation;

/// <summary>
/// Checks that a written conversation contains the same script and texts as the original file.
/// Only the text table layout may differ, e.g. files edited by the old tool keep extra zero padding after each text.
/// </summary>
public static class Equivalence
{
    private const int CsvbSignatureLength = 0x0C;
    private const int CsvbHeaderPaddingStart = 0x1C;
    private const int MapiHeaderStart = 0x20;

    public static bool IsEquivalent(byte[] original, ConversationCsvb fromOriginal, byte[] written, ConversationCsvb fromWritten)
    {
        // bytes the reader does not keep but the writer writes as constants
        if (!original.AsSpan(0, CsvbSignatureLength).SequenceEqual(written.AsSpan(0, CsvbSignatureLength))
            || !original.AsSpan(CsvbHeaderPaddingStart, MapiHeaderStart - CsvbHeaderPaddingStart).SequenceEqual(written.AsSpan(CsvbHeaderPaddingStart, MapiHeaderStart - CsvbHeaderPaddingStart))
            || !HasEndOfPointerTableMarker(original, fromOriginal))
        {
            return false;
        }

        if (!fromOriginal.MapiHeader.FullHeaderBytes.AsSpan().SequenceEqual(fromWritten.MapiHeader.FullHeaderBytes)
            || !fromOriginal.OffsetTablePrefix.AsSpan().SequenceEqual(fromWritten.OffsetTablePrefix)
            || !fromOriginal.AfterTextSectionBytes.AsSpan().SequenceEqual(fromWritten.AfterTextSectionBytes)
            || fromOriginal.Pointers.Count != fromWritten.Pointers.Count)
        {
            return false;
        }

        return fromOriginal.Pointers.Zip(fromWritten.Pointers).All(x => IsSameGroup(x.First, x.Second));
    }

    private static bool IsSameGroup(ConversationPointer a, ConversationPointer b)
    {
        if (!a.Group.Select(x => x.Type).SequenceEqual(b.Group.Select(x => x.Type)) || a.Slot.Length != b.Slot.Length)
        {
            return false;
        }

        var textA = a.Group.OfType<ConversationPointerText>().FirstOrDefault();
        var textB = b.Group.OfType<ConversationPointerText>().FirstOrDefault();
        if (textA == null || textB == null)
        {
            return textA == textB && a.Slot.AsSpan().SequenceEqual(b.Slot);
        }

        // the first 4 bytes are the text offset, which depends on the text table layout
        return textA.TextLines == textB.TextLines && a.Slot.AsSpan(4).SequenceEqual(b.Slot.AsSpan(4));
    }

    private static bool HasEndOfPointerTableMarker(byte[] original, ConversationCsvb fromOriginal)
    {
        var header = fromOriginal.CsvbHeader;
        return header.OffsetToEndOfFullHeader - header.OffsetToEndOfPointerTable == 8
            && original.AsSpan(header.OffsetToEndOfPointerTable, 8).SequenceEqual(new byte[] { 2, 0, 0, 0, 2, 0, 0, 0 });
    }
}