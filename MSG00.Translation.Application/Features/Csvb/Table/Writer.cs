using MSG00.Translation.Application.Features.Font;
using System.Text;

namespace MSG00.Translation.Application.Features.Csvb.Table;

public interface IWriter
{
    Task WriteFileAsync(Stream stream, TableCsvb tableCsvb);
}

/// <summary>
/// Writes changed texts without moving any existing data: in place when the new text fits into the bytes of the old
/// one, otherwise appended to the end of the text table (the cell then points to it). The data after the text table
/// is not fully understood yet and contains values that look like text offsets, so existing texts must stay where they are.
/// </summary>
public class Writer : IWriter
{
    private const int OffsetToEndOfTextTable = 0x14;
    private const int OffsetToEndOfExtraTable = 0x18;

    private static readonly Encoding ShiftJis = GameEncoding.Instance;

    public async Task WriteFileAsync(Stream stream, TableCsvb tableCsvb)
    {
        var bytes = new List<byte>(tableCsvb.Bytes);
        var fullHeaderEnd = tableCsvb.CsvbHeader.OffsetToEndOfFullHeader;

        foreach (var text in tableCsvb.Texts.Where(x => x.IsChanged))
        {
            var newBytes = ShiftJis.GetBytes(text.Text);
            var room = Padded(ShiftJis.GetByteCount(text.Original));
            var shared = tableCsvb.Texts.Count(x => x.TextOffset == text.TextOffset) > 1;

            if (newBytes.Length < room && !shared)
            {
                var position = fullHeaderEnd + text.TextOffset;
                for (var i = 0; i < room; i++)
                {
                    bytes[position + i] = i < newBytes.Length ? newBytes[i] : (byte)0;
                }

                continue;
            }

            var textTableEnd = ReadInt(bytes, OffsetToEndOfTextTable);
            var block = new byte[Padded(newBytes.Length)];
            newBytes.CopyTo(block, 0);
            bytes.InsertRange(textTableEnd, block);

            WriteInt(bytes, text.CellOffset, textTableEnd - fullHeaderEnd);
            WriteInt(bytes, OffsetToEndOfTextTable, textTableEnd + block.Length);
            WriteInt(bytes, OffsetToEndOfExtraTable, ReadInt(bytes, OffsetToEndOfExtraTable) + block.Length);
        }

        stream.SetLength(0);
        await stream.WriteAsync(bytes.ToArray()).ConfigureAwait(false);
    }

    /// <summary>text + NUL, padded to 4-byte alignment (always 1–4 zero bytes)</summary>
    private static int Padded(int length) => length + (4 - length % 4);

    private static int ReadInt(List<byte> bytes, int offset) => BitConverter.ToInt32([bytes[offset], bytes[offset + 1], bytes[offset + 2], bytes[offset + 3]]);

    private static void WriteInt(List<byte> bytes, int offset, int value)
    {
        var valueBytes = BitConverter.GetBytes(value);
        for (var i = 0; i < 4; i++)
        {
            bytes[offset + i] = valueBytes[i];
        }
    }
}