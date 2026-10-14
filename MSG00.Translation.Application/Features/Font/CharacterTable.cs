using System.Text;

namespace MSG00.Translation.Application.Features.Font;

/// <summary>
/// Extra characters of the game font (e.g. ä, é, ñ): each one replaces a half-width katakana of Shift-JIS, so it is
/// 1 byte in the texts and as wide as a letter. The table travels with the ROM in the spare bits of the font
/// (<see cref="ToBytes"/>). See docs/font/README.md.
/// </summary>
public sealed class CharacterTable
{
    /// <summary>half-width katakana ｡ … ﾟ: 1-byte characters of Shift-JIS, 6 px wide in the game font</summary>
    public const byte FirstSlot = 0xA1;

    public const byte LastSlot = 0xDF;

    /// <summary>･ (half-width middle dot) is used by the Japanese cut-in texts and stays as it is</summary>
    public static readonly IReadOnlySet<byte> ReservedSlots = new HashSet<byte> { 0xA5 };

    /// <summary>slots that can take a character, in order</summary>
    public static IReadOnlyList<byte> Slots { get; } = Enumerable.Range(FirstSlot, LastSlot - FirstSlot + 1)
        .Select(x => (byte)x)
        .Where(x => !ReservedSlots.Contains(x))
        .ToList();

    public static CharacterTable Empty { get; } = new(new Dictionary<byte, char>());

    private static readonly byte[] Magic = "MSGC"u8.ToArray();
    private const byte Version = 1;
    private const int HeaderSize = 6;

    private static readonly Encoding StrictShiftJis;

    static CharacterTable()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        StrictShiftJis = Encoding.GetEncoding("Shift-JIS", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
    }

    private readonly Dictionary<char, byte> _slotOf;

    public CharacterTable(IReadOnlyDictionary<byte, char> characters)
    {
        foreach (var (slot, character) in characters)
        {
            if (!Slots.Contains(slot))
            {
                throw new ArgumentException($"0x{slot:X2} is not a free slot");
            }

            if (Check(character) is { } reason)
            {
                throw new ArgumentException($"{character}: {reason}");
            }
        }

        Characters = characters.ToDictionary(x => x.Key, x => x.Value);
        _slotOf = Characters.GroupBy(x => x.Value).ToDictionary(
            x => x.Key,
            x => x.Count() == 1 ? x.Single().Key : throw new ArgumentException($"{x.Key} is in more than one slot"));
    }

    /// <summary>slot (byte in the texts) → character</summary>
    public IReadOnlyDictionary<byte, char> Characters { get; }

    public bool IsEmpty => Characters.Count == 0;

    public bool TryGetSlot(char character, out byte slot) => _slotOf.TryGetValue(character, out slot);

    public bool TryGetCharacter(byte slot, out char character) => Characters.TryGetValue(slot, out character);

    /// <returns>why the character can not be added, null when it can</returns>
    public static string? Check(char character)
    {
        if (char.IsSurrogate(character))
        {
            return "characters outside the Basic Multilingual Plane are not supported";
        }

        if (char.IsControl(character) || char.IsWhiteSpace(character))
        {
            return "not a visible character";
        }

        if (character < 0x80)
        {
            return "already in the game font";
        }

        return CanShiftJisEncode(character) ? "already in the game font (full-width)" : null;
    }

    /// <summary>true when Shift-JIS has the character (then the original font has it too)</summary>
    public static bool CanShiftJisEncode(char character)
    {
        try
        {
            StrictShiftJis.GetBytes([character]);
            return true;
        }
        catch (EncoderFallbackException)
        {
            return false;
        }
    }

    /// <summary>"MSGC", version, count, then per character: slot, UTF-16 code (little endian)</summary>
    public byte[] ToBytes()
    {
        var bytes = new List<byte>(Magic) { Version, (byte)Characters.Count };
        foreach (var (slot, character) in Characters.OrderBy(x => x.Key))
        {
            bytes.Add(slot);
            bytes.AddRange(BitConverter.GetBytes((ushort)character));
        }

        return bytes.ToArray();
    }

    public static int SizeOf(int characters) => HeaderSize + characters * 3;

    /// <summary>reads a table written by <see cref="ToBytes"/>; anything else (e.g. the original font) gives <see cref="Empty"/></summary>
    public static CharacterTable FromBytes(Func<int, byte[]> read)
    {
        var header = read(HeaderSize);
        if (header.Length < HeaderSize || !header.AsSpan(0, 4).SequenceEqual(Magic) || header[4] != Version)
        {
            return Empty;
        }

        var bytes = read(SizeOf(header[5]));
        if (bytes.Length < SizeOf(header[5]))
        {
            return Empty;
        }

        var characters = new Dictionary<byte, char>();
        for (var i = 0; i < header[5]; i++)
        {
            var entry = HeaderSize + i * 3;
            characters[bytes[entry]] = (char)BitConverter.ToUInt16(bytes, entry + 1);
        }

        try
        {
            return new CharacterTable(characters);
        }
        catch (ArgumentException)
        {
            return Empty;
        }
    }
}