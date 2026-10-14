using MSG00.Translation.Application.Features.Font;
using System.Text;
using MSG00.Translation.Application.Features.Csvb.Mapi;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll;

public interface IReader
{
    Task<StaffRollTextCsvb> ReadFileAsync(MapiCsvb mapiCsvb, Stream stream);
}

/// <summary>
/// Reads the pointer table (8-byte entries {slot offset, type} from 0x50 to OffsetToEndOfPointerTable) and the texts
/// of the <see cref="TextPointerType"/> entries: the first slot word is the text offset.
/// </summary>
internal sealed class Reader : IReader
{
    public const int TextPointerType = 0x08;
    public const int TextOffsetInSlot = 0;

    private const int PointerTableStart = 0x50;

    private static readonly Encoding ShiftJis = GameEncoding.Instance;

    public async Task<StaffRollTextCsvb> ReadFileAsync(MapiCsvb mapiCsvb, Stream stream)
    {
        var bytes = new byte[stream.Length];
        stream.Position = 0;
        await stream.ReadExactlyAsync(bytes).ConfigureAwait(false);

        var header = mapiCsvb.CsvbHeader;
        var dataArea = header.OffsetToEndOfExtraTable;
        var texts = new List<StaffRollText>();
        for (var p = PointerTableStart; p + 8 <= header.OffsetToEndOfPointerTable; p += 8)
        {
            if (BitConverter.ToInt32(bytes, p + 4) != TextPointerType)
            {
                continue;
            }

            var slot = BitConverter.ToInt32(bytes, p);
            var textOffset = BitConverter.ToInt32(bytes, dataArea + slot + TextOffsetInSlot);
            var text = ReadText(bytes, header.OffsetToEndOfFullHeader + textOffset);
            texts.Add(new StaffRollText
            {
                SlotOffset = slot,
                TextOffset = textOffset,
                Parameter = dataArea + slot + 8 <= bytes.Length ? BitConverter.ToInt32(bytes, dataArea + slot + 4) : 0,
                Original = text,
                Text = text,
            });
        }

        return new StaffRollTextCsvb { CsvbHeader = header, MapiHeader = mapiCsvb.MapiHeader, Bytes = bytes, Texts = texts };
    }

    private static string ReadText(byte[] bytes, int offset)
    {
        var end = Array.IndexOf(bytes, (byte)0, offset);
        return ShiftJis.GetString(bytes, offset, end - offset);
    }
}