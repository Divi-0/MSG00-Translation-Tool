using MSG00.Translation.Application.Features.Csvb.Mapi;
using MSG00.Translation.Application.Features.Csvb.Read;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.Conversation;

public interface IReader
{
    Task<ConversationCsvb> ReadFileAsync(MapiCsvb mapiCsvb, Stream stream);
}

internal sealed class Reader : CsvbMapiReader, IReader
{
    private const int PointerTableStart = 0x50;

    public async Task<ConversationCsvb> ReadFileAsync(MapiCsvb mapiCsvb, Stream stream)
    {
        var fileBytes = new byte[stream.Length];
        stream.Position = 0;
        await stream.ReadExactlyAsync(fileBytes).ConfigureAwait(false);

        var header = mapiCsvb.CsvbHeader;
        var offsetTableStart = header.OffsetToEndOfExtraTable;

        var entries = new List<(int Slot, ConversationPointerType Type)>();
        for (var i = PointerTableStart; i < header.OffsetToEndOfPointerTable; i += 8)
        {
            entries.Add((BitConverter.ToInt32(fileBytes, i), (ConversationPointerType)BitConverter.ToInt32(fileBytes, i + 4)));
        }

        var slotOffsets = entries.Select(x => x.Slot).Distinct().ToList();
        var firstSlot = slotOffsets.Count > 0 ? slotOffsets[0] : fileBytes.Length - offsetTableStart;

        var conversationCsvb = new ConversationCsvb
        {
            CsvbHeader = header,
            MapiHeader = mapiCsvb.MapiHeader,
            AfterTextSectionBytes = await GetConstBytesAfterTextTable(stream, BitConverter.ToInt32(header.OffsetToEndOfTextTable), offsetTableStart).ConfigureAwait(false),
            OffsetTablePrefix = fileBytes[offsetTableStart..(offsetTableStart + firstSlot)],
        };

        foreach (var group in entries.GroupBy(x => x.Slot))
        {
            var index = slotOffsets.IndexOf(group.Key);
            var slotEnd = index + 1 < slotOffsets.Count ? slotOffsets[index + 1] : fileBytes.Length - offsetTableStart;
            var slot = fileBytes[(offsetTableStart + group.Key)..(offsetTableStart + slotEnd)];

            ConversationPointer? head = null;
            foreach (var (_, type) in group)
            {
                ConversationPointer pointer = IsText(type)
                    ? new ConversationPointerText { Type = type, TextLines = GetSingleTextFromFile(conversationCsvb, fileBytes, BitConverter.ToInt32(slot, 0)) }
                    : new ConversationPointer { Type = type };

                if (head == null)
                {
                    head = pointer;
                    head.Slot = slot;
                    conversationCsvb.Pointers.Add(head);
                }
                else
                {
                    head.SubPointer.Add(pointer);
                }
            }
        }

        return conversationCsvb;
    }

    internal static bool IsText(ConversationPointerType type) =>
        type is ConversationPointerType.Text or ConversationPointerType.TextWithParameters or ConversationPointerType.Question;
}