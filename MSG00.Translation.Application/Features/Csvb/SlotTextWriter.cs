using MSG00.Translation.Application.Features.Font;
using System.Text;

namespace MSG00.Translation.Application.Features.Csvb;

/// <summary>A text referenced from a command slot in the data area of a script csvb (mission scripts, cut-ins).</summary>
/// <param name="SlotOffset">slot offset relative to the data area (OffsetToEndOfExtraTable)</param>
/// <param name="WordOffset">byte offset of the text offset inside the slot</param>
/// <param name="TextOffset">offset in the text table</param>
public sealed record SlotText(int SlotOffset, int WordOffset, int TextOffset, string Original, string Text)
{
    public bool IsChanged => Text != Original;
}

/// <summary>
/// Writes changed texts of a script csvb without moving existing data: in place when the new text fits into the bytes
/// of the old one and no other reference uses that text with a different wording, otherwise appended to the end of the
/// text table and the slot points to it. The text table end (0x14) and the data area start (0x18) grow by the appended
/// size; slots are relative to the data area, so they stay valid.
/// </summary>
public static class SlotTextWriter
{
    private const int OffsetToEndOfTextTable = 0x14;
    private const int OffsetToEndOfExtraTable = 0x18;

    private static readonly Encoding ShiftJis = GameEncoding.Instance;

    /// <param name="texts">all text references of the file, changed or not</param>
    /// <param name="protectedTextOffsets">texts used by other references (e.g. labels) that must never be overwritten</param>
    public static byte[] Write(byte[] original, int fullHeaderEnd, IReadOnlyList<SlotText> texts, IReadOnlySet<int> protectedTextOffsets)
    {
        var bytes = new List<byte>(original);
        var appended = new List<(SlotText Text, byte[] Bytes)>();

        foreach (var text in texts.Where(x => x.IsChanged))
        {
            var newBytes = ShiftJis.GetBytes(text.Text);
            var room = Padded(ShiftJis.GetByteCount(text.Original));
            var sharedWithOtherText = protectedTextOffsets.Contains(text.TextOffset)
                || texts.Any(x => x.TextOffset == text.TextOffset && x.Text != text.Text);

            if (newBytes.Length < room && !sharedWithOtherText)
            {
                var position = fullHeaderEnd + text.TextOffset;
                for (var i = 0; i < room; i++)
                {
                    bytes[position + i] = i < newBytes.Length ? newBytes[i] : (byte)0;
                }

                continue;
            }

            appended.Add((text, newBytes));
        }

        if (appended.Count == 0)
        {
            return bytes.ToArray();
        }

        var textTableEnd = ReadInt(bytes, OffsetToEndOfTextTable);
        var block = new List<byte>();
        var newOffsets = new List<int>();
        foreach (var (_, textBytes) in appended)
        {
            newOffsets.Add(textTableEnd - fullHeaderEnd + block.Count);
            block.AddRange(textBytes);
            block.AddRange(new byte[4 - textBytes.Length % 4]);
        }

        bytes.InsertRange(textTableEnd, block);
        WriteInt(bytes, OffsetToEndOfTextTable, textTableEnd + block.Count);
        var dataAreaStart = ReadInt(bytes, OffsetToEndOfExtraTable) + block.Count;
        WriteInt(bytes, OffsetToEndOfExtraTable, dataAreaStart);

        for (var i = 0; i < appended.Count; i++)
        {
            WriteInt(bytes, dataAreaStart + appended[i].Text.SlotOffset + appended[i].Text.WordOffset, newOffsets[i]);
        }

        return bytes.ToArray();
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