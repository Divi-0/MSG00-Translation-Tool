using MSG00.Translation.Application.Features.Csvb.Write;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.Conversation;

public interface IWriter
{
    Task WriteFileAsync(Stream stream, ConversationCsvb conversationCsvb);
}

/// <summary>
/// Rebuilds the text table in pointer order and updates the text offsets; all other slots are written unchanged.
/// </summary>
public class Writer : CsvbWriter, IWriter
{
    public async Task WriteFileAsync(Stream stream, ConversationCsvb conversationCsvb)
    {
        stream.SetLength(0);
        await CreateCsvbHeaderAsync(stream).ConfigureAwait(false);
        await stream.WriteAsync(CreateMapiHeader(conversationCsvb)).ConfigureAwait(false);

        await WritePointerTableAsync(stream, conversationCsvb).ConfigureAwait(false);
        var offsetToEndOfPointerTable = Convert.ToInt32(stream.Length);

        await stream.WriteAsync(new byte[] { 0x02, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
        var offsetToEndOfFullHeader = Convert.ToInt32(stream.Length);

        await WriteTextTableAsync(stream, conversationCsvb).ConfigureAwait(false);
        var offsetToEndOfTextTable = Convert.ToInt32(stream.Length);

        await stream.WriteAsync(conversationCsvb.AfterTextSectionBytes).ConfigureAwait(false);
        var offsetToEndOfExtraTable = Convert.ToInt32(stream.Length);

        await stream.WriteAsync(conversationCsvb.OffsetTablePrefix).ConfigureAwait(false);
        foreach (var pointer in conversationCsvb.Pointers)
        {
            await stream.WriteAsync(pointer.Slot).ConfigureAwait(false);
        }

        await OverrideCsvbHeaderAsync(stream, offsetToEndOfPointerTable, offsetToEndOfFullHeader, offsetToEndOfTextTable, offsetToEndOfExtraTable).ConfigureAwait(false);
    }

    /// <summary>
    /// MAPI header 0x34 (header byte 0x14) holds the number of pointer table entries + 1; it changes when text boxes are added.
    /// </summary>
    private static byte[] CreateMapiHeader(ConversationCsvb conversationCsvb)
    {
        const int pointerCountOffset = 0x14;

        var header = (byte[])conversationCsvb.MapiHeader.FullHeaderBytes.Clone();
        var entryCount = conversationCsvb.Pointers.Sum(x => x.Group.Count());
        BitConverter.GetBytes(entryCount + 1).CopyTo(header, pointerCountOffset);
        return header;
    }

    private static async Task WritePointerTableAsync(Stream stream, ConversationCsvb conversationCsvb)
    {
        var slotOffset = conversationCsvb.OffsetTablePrefix.Length;
        foreach (var pointer in conversationCsvb.Pointers)
        {
            foreach (var entry in pointer.Group)
            {
                await stream.WriteAsync(BitConverter.GetBytes(slotOffset)).ConfigureAwait(false);
                await stream.WriteAsync(BitConverter.GetBytes((int)entry.Type)).ConfigureAwait(false);
            }

            slotOffset += pointer.Slot.Length;
        }
    }

    private async Task WriteTextTableAsync(Stream stream, ConversationCsvb conversationCsvb)
    {
        var textOffset = 4;
        await stream.WriteAsync(new byte[] { 0, 0, 0, 0 }).ConfigureAwait(false);

        foreach (var pointer in conversationCsvb.Pointers)
        {
            if (pointer.Group.OfType<ConversationPointerText>().FirstOrDefault() is not { } text)
            {
                continue;
            }

            BitConverter.GetBytes(textOffset).CopyTo(pointer.Slot, 0);

            var textBytes = _shiftJISEncoding.GetBytes(text.TextLines).AddTrailingCsvbFileZeroes();
            await stream.WriteAsync(textBytes).ConfigureAwait(false);
            textOffset += textBytes.Length;
        }
    }
}