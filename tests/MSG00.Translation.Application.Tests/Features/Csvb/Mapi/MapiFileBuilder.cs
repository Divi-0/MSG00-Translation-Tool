using System.Text;

namespace MSG00.Translation.Application.Tests.Features.Csvb.Mapi;

/// <summary>
/// Builds a synthetic MAPI csvb with the layout of the game files (see docs/conversation/README.md):
/// CSVB header, MAPI header, pointer table, 02/02 marker, text table, extra section, offset table.
/// </summary>
internal sealed class MapiFileBuilder
{
    public const int ConversationType = 0x10;
    public const int PrologueEpilogueType = 0x0A;
    public const int StaffRollType = 0x0D;

    /// <summary>the 76 byte section after the text table is identical in all conversation files</summary>
    public static readonly byte[] ExtraSection = Enumerable.Range(0, 76).Select(i => (byte)(i * 7)).ToArray();

    private static readonly Encoding ShiftJis = Encoding.GetEncoding("Shift-JIS");

    private readonly int _mapiType;
    private readonly byte[] _offsetTablePrefix;
    private readonly List<(int[] Types, byte[] Slot, string? Text, int ExtraZeros)> _groups = new();

    public MapiFileBuilder(int mapiType = ConversationType, byte[]? offsetTablePrefix = null)
    {
        _mapiType = mapiType;
        _offsetTablePrefix = offsetTablePrefix ?? new byte[] { 0x0A, 0, 0, 0 };
    }

    /// <summary>group with raw slot data, e.g. a character (8 bytes) or an empty end slot</summary>
    public MapiFileBuilder Group(byte[] slot, params int[] types)
    {
        _groups.Add((types, slot, null, 0));
        return this;
    }

    /// <param name="leadingTypes">types in the same group before the character, e.g. 0x09 (yes branch)</param>
    public MapiFileBuilder Character(int id, int variant, params int[] leadingTypes) =>
        Group(BitConverter.GetBytes(id).Concat(BitConverter.GetBytes(variant)).ToArray(), leadingTypes.Append(0x00).ToArray());

    /// <summary>group whose 4 byte slot holds the offset of <paramref name="text"/></summary>
    /// <param name="extraZeros">additional zero padding after the text, like files written by the old tool</param>
    public MapiFileBuilder Text(string text, int[] types, int extraZeros = 0)
    {
        _groups.Add((types, new byte[4], text, extraZeros));
        return this;
    }

    public MapiFileBuilder Text(string text) => Text(text, new[] { 0x01 });

    public byte[] Build()
    {
        var entryCount = _groups.Sum(x => x.Types.Length);

        // text table and text offsets
        var textTable = new List<byte> { 0, 0, 0, 0 };
        var slots = new List<byte[]>();
        foreach (var (_, slot, text, extraZeros) in _groups)
        {
            var copy = (byte[])slot.Clone();
            if (text != null)
            {
                BitConverter.GetBytes(textTable.Count).CopyTo(copy, 0);
                var bytes = ShiftJis.GetBytes(text);
                textTable.AddRange(bytes);
                textTable.AddRange(new byte[4 - bytes.Length % 4 + extraZeros]);
            }

            slots.Add(copy);
        }

        var pointerTable = new List<byte>();
        var slotOffset = _offsetTablePrefix.Length;
        for (var i = 0; i < _groups.Count; i++)
        {
            foreach (var type in _groups[i].Types)
            {
                pointerTable.AddRange(BitConverter.GetBytes(slotOffset));
                pointerTable.AddRange(BitConverter.GetBytes(type));
            }

            slotOffset += slots[i].Length;
        }

        var file = new List<byte>();
        file.AddRange("CSVBv4.3"u8.ToArray());
        file.AddRange(new byte[] { 1, 0, 0, 0 });
        file.AddRange(new byte[20]); // four section offsets + padding, patched below
        file.AddRange(MapiHeader(_mapiType, entryCount + 1));
        file.AddRange(pointerTable);
        var endOfPointerTable = file.Count;
        file.AddRange(new byte[] { 2, 0, 0, 0, 2, 0, 0, 0 });
        var endOfFullHeader = file.Count;
        file.AddRange(textTable);
        var endOfTextTable = file.Count;
        file.AddRange(ExtraSection);
        var endOfExtraTable = file.Count;
        file.AddRange(_offsetTablePrefix);
        foreach (var slot in slots)
        {
            file.AddRange(slot);
        }

        var result = file.ToArray();
        BitConverter.GetBytes(endOfPointerTable).CopyTo(result, 0x0C);
        BitConverter.GetBytes(endOfFullHeader).CopyTo(result, 0x10);
        BitConverter.GetBytes(endOfTextTable).CopyTo(result, 0x14);
        BitConverter.GetBytes(endOfExtraTable).CopyTo(result, 0x18);
        return result;
    }

    /// <summary>48-byte MAPI header with the field values of the game's conversation files</summary>
    public static byte[] MapiHeader(int mapiType, int pointerCount)
    {
        var header = new byte[48];
        "MAPI"u8.ToArray().CopyTo(header, 0);
        BitConverter.GetBytes(1).CopyTo(header, 0x10);
        BitConverter.GetBytes(pointerCount).CopyTo(header, 0x14);
        BitConverter.GetBytes(8).CopyTo(header, 0x18);
        BitConverter.GetBytes(2).CopyTo(header, 0x1C);
        BitConverter.GetBytes(0x48).CopyTo(header, 0x24);
        BitConverter.GetBytes(mapiType).CopyTo(header, 0x2C);
        return header;
    }

    /// <summary>
    /// A conversation like C1_003: character, text, yes/no question with both branches, final branch end without slot data.
    /// </summary>
    public static MapiFileBuilder ConversationWithChoice(int extraZeros = 0) => new MapiFileBuilder()
        .Character(5, 2)
        .Text("演習はまずまずだったようね。\n二行目", new[] { 0x01 }, extraZeros)
        .Text("説明を受けますか？\n（ＹＥＳ／ＮＯ）", new[] { 0x05 }, extraZeros)
        .Character(5, 1, 0x09)
        .Text("YES branch text", new[] { 0x01 }, extraZeros)
        .Group(BitConverter.GetBytes(5).Concat(BitConverter.GetBytes(1)).ToArray(), 0x0D, 0x0A, 0x00)
        .Text("NO branch text", new[] { 0x01 }, extraZeros)
        .Group(Array.Empty<byte>(), 0x0D);
}