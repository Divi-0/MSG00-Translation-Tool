using System.Text;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.CutIn;

public interface IReader
{
    Task<CutInCsvb> ReadFileAsync(CsvbFile csvbFile, Stream stream);
}

/// <summary>
/// Reads the table descriptors at 0x20 (until the first table's rows), all {slot, command} rows and the lines of the
/// 0x34 commands (slot words {fade, display time, text offset}).
/// </summary>
internal sealed class Reader : IReader
{
    public const int LineCommand = 0x34;
    public const int TextOffsetInSlot = 8;

    private const int FirstDescriptor = 0x20;
    private const int DescriptorSize = 40;

    private static readonly Encoding ShiftJis = Encoding.GetEncoding("Shift-JIS");

    public async Task<CutInCsvb> ReadFileAsync(CsvbFile csvbFile, Stream stream)
    {
        var bytes = new byte[stream.Length];
        stream.Position = 0;
        await stream.ReadExactlyAsync(bytes).ConfigureAwait(false);

        var header = csvbFile.CsvbHeader;
        var dataArea = header.OffsetToEndOfExtraTable;
        var lines = new List<CutInLine>();

        var firstRows = int.MaxValue;
        for (var p = FirstDescriptor; p < firstRows; p += DescriptorSize)
        {
            var table = Encoding.ASCII.GetString(bytes, p, 16).TrimEnd('\0');
            var rows = BitConverter.ToInt32(bytes, p + 20);
            var rowSize = BitConverter.ToInt32(bytes, p + 24);
            var start = BitConverter.ToInt32(bytes, p + 36);
            firstRows = Math.Min(firstRows, start);

            for (var row = 0; row < rows; row++)
            {
                var slot = BitConverter.ToInt32(bytes, start + row * rowSize);
                var command = BitConverter.ToInt32(bytes, start + row * rowSize + 4);
                if (command != LineCommand)
                {
                    continue;
                }

                var textOffset = BitConverter.ToInt32(bytes, dataArea + slot + TextOffsetInSlot);
                var text = ReadText(bytes, header.OffsetToEndOfFullHeader + textOffset);
                lines.Add(new CutInLine
                {
                    Table = table,
                    SlotOffset = slot,
                    TextOffset = textOffset,
                    DisplayTime = BitConverter.ToInt32(bytes, dataArea + slot + 4),
                    Original = text,
                    Text = text,
                });
            }
        }

        return new CutInCsvb { CsvbHeader = header, Bytes = bytes, Lines = lines };
    }

    private static string ReadText(byte[] bytes, int offset)
    {
        var end = Array.IndexOf(bytes, (byte)0, offset);
        return ShiftJis.GetString(bytes, offset, end - offset);
    }
}