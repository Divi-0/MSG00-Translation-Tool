using System.Text;

namespace MSG00.Translation.Application.Features.Font;

/// <summary>
/// The encoding of all game texts: Shift-JIS, plus the extra characters of the font (<see cref="CharacterTable"/>)
/// in place of half-width katakana. <see cref="Instance"/> follows the character table of the opened ROM.
/// </summary>
public sealed class GameEncoding : Encoding
{
    private static readonly Encoding ShiftJis;

    static GameEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ShiftJis = Encoding.GetEncoding("Shift-JIS");
    }

    /// <summary>used by all readers, writers and editors; its table is set when a ROM is opened or a font is applied</summary>
    public static GameEncoding Instance { get; } = new();

    private volatile CharacterTable _characters;

    public GameEncoding(CharacterTable? characters = null)
    {
        _characters = characters ?? CharacterTable.Empty;
    }

    public CharacterTable Characters
    {
        get => _characters;
        set => _characters = value;
    }

    public override string WebName => ShiftJis.WebName;

    public override string EncodingName => $"{ShiftJis.EncodingName} + game font characters";

    /// <summary>characters of the text the game font can not show (they would be written as '?' or a look-alike)</summary>
    public IReadOnlyList<char> FindMissingCharacters(string text) => text
        .Where(c => c is not ('\n' or '\r') && !char.IsSurrogate(c) && c >= 0x80 && !_characters.TryGetSlot(c, out _) && !CharacterTable.CanShiftJisEncode(c))
        .Distinct()
        .ToList();

    public override int GetByteCount(char[] chars, int index, int count)
    {
        var table = _characters;
        if (table.IsEmpty)
        {
            return ShiftJis.GetByteCount(chars, index, count);
        }

        var total = 0;
        foreach (var (start, length, slot) in CharRuns(chars, index, count, table))
        {
            total += slot.HasValue ? 1 : ShiftJis.GetByteCount(chars, start, length);
        }

        return total;
    }

    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    {
        var table = _characters;
        if (table.IsEmpty)
        {
            return ShiftJis.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
        }

        var position = byteIndex;
        foreach (var (start, length, slot) in CharRuns(chars, charIndex, charCount, table))
        {
            if (slot.HasValue)
            {
                bytes[position++] = slot.Value;
            }
            else
            {
                position += ShiftJis.GetBytes(chars, start, length, bytes, position);
            }
        }

        return position - byteIndex;
    }

    public override int GetCharCount(byte[] bytes, int index, int count)
    {
        var table = _characters;
        if (table.IsEmpty)
        {
            return ShiftJis.GetCharCount(bytes, index, count);
        }

        var total = 0;
        foreach (var (start, length, character) in ByteRuns(bytes, index, count, table))
        {
            total += character.HasValue ? 1 : ShiftJis.GetCharCount(bytes, start, length);
        }

        return total;
    }

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    {
        var table = _characters;
        if (table.IsEmpty)
        {
            return ShiftJis.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
        }

        var position = charIndex;
        foreach (var (start, length, character) in ByteRuns(bytes, byteIndex, byteCount, table))
        {
            if (character.HasValue)
            {
                chars[position++] = character.Value;
            }
            else
            {
                position += ShiftJis.GetChars(bytes, start, length, chars, position);
            }
        }

        return position - charIndex;
    }

    public override int GetMaxByteCount(int charCount) => ShiftJis.GetMaxByteCount(charCount);

    public override int GetMaxCharCount(int byteCount) => ShiftJis.GetMaxCharCount(byteCount);

    /// <summary>splits the text into single extra characters (with their slot) and runs of plain Shift-JIS text</summary>
    private static IEnumerable<(int Start, int Length, byte? Slot)> CharRuns(char[] chars, int index, int count, CharacterTable table)
    {
        var start = index;
        for (var i = index; i < index + count; i++)
        {
            if (table.TryGetSlot(chars[i], out var slot))
            {
                if (i > start)
                {
                    yield return (start, i - start, null);
                }

                yield return (i, 1, slot);
                start = i + 1;
            }
        }

        if (index + count > start)
        {
            yield return (start, index + count - start, null);
        }
    }

    /// <summary>
    /// Splits the bytes into extra characters and runs of plain Shift-JIS; lead bytes (0x81–0x9F, 0xE0–0xFC) start a
    /// 2-byte character, so a trail byte is never taken for an extra character.
    /// </summary>
    private static IEnumerable<(int Start, int Length, char? Character)> ByteRuns(byte[] bytes, int index, int count, CharacterTable table)
    {
        var start = index;
        var end = index + count;
        for (var i = index; i < end;)
        {
            var b = bytes[i];
            if (b is >= 0x81 and <= 0x9F or >= 0xE0 and <= 0xFC)
            {
                i += 2;
                continue;
            }

            if (table.TryGetCharacter(b, out var character))
            {
                if (i > start)
                {
                    yield return (start, i - start, null);
                }

                yield return (i, 1, character);
                start = i + 1;
            }

            i++;
        }

        if (end > start)
        {
            yield return (start, end - start, null);
        }
    }
}