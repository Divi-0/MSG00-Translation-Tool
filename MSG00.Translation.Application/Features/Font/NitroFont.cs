namespace MSG00.Translation.Application.Features.Font;

/// <summary>
/// A Nitro font (NFTR) inside a byte array, e.g. LC_font_12.NFTR inside a NARC. Glyphs are read and written in
/// place: the font keeps its size and its character map. Blocks: FINF (info), CGLP (glyph bitmaps), CWDH (widths),
/// CMAP (character code → glyph). See docs/font/README.md.
/// </summary>
public sealed class NitroFont
{
    private readonly byte[] _data;
    private readonly int _glyphs;
    private readonly int _cellSize;
    private readonly int _bitsPerPixel;
    private readonly List<(int First, int Last, int Entries)> _widths = new();
    private readonly Dictionary<int, int> _glyphOfCode = new();

    /// <param name="offset">start of the NFTR ("RTFN") in <paramref name="data"/></param>
    public NitroFont(byte[] data, int offset)
    {
        if (!data.AsSpan(offset, 4).SequenceEqual("RTFN"u8))
        {
            throw new InvalidDataException("not a Nitro font (NFTR)");
        }

        _data = data;
        var finf = offset + BitConverter.ToUInt16(data, offset + 0x0C);
        Encoding = data[finf + 15];

        var cglp = offset + BitConverter.ToInt32(data, finf + 16) - 8;
        CellWidth = data[cglp + 8];
        CellHeight = data[cglp + 9];
        _cellSize = BitConverter.ToUInt16(data, cglp + 10);
        _bitsPerPixel = data[cglp + 14];
        _glyphs = cglp + 16;
        GlyphCount = (BitConverter.ToInt32(data, cglp + 4) - 16) / _cellSize;

        for (var cwdh = offset + BitConverter.ToInt32(data, finf + 20) - 8; ;)
        {
            _widths.Add((BitConverter.ToUInt16(data, cwdh + 8), BitConverter.ToUInt16(data, cwdh + 10), cwdh + 16));
            var next = BitConverter.ToInt32(data, cwdh + 12);
            if (next == 0)
            {
                break;
            }

            cwdh = offset + next - 8;
        }

        for (var cmap = offset + BitConverter.ToInt32(data, finf + 24) - 8; ;)
        {
            ReadCharacterMap(cmap);
            var next = BitConverter.ToInt32(data, cmap + 16);
            if (next == 0)
            {
                break;
            }

            cmap = offset + next - 8;
        }
    }

    /// <summary>0 = UTF-8, 1 = UTF-16, 2 = Shift-JIS, 3 = Windows-1252</summary>
    public int Encoding { get; }

    public int CellWidth { get; }

    public int CellHeight { get; }

    public int GlyphCount { get; }

    /// <summary>bits at the end of every glyph cell that are not part of the picture (7 for 11 × 11 at 1 bpp)</summary>
    public int SpareBitsPerGlyph => _cellSize * 8 - CellWidth * CellHeight * _bitsPerPixel;

    /// <summary>bytes that fit into the spare bits of all glyphs, see <see cref="ReadSpareBytes"/></summary>
    public int SpareByteCapacity => GlyphCount * SpareBitsPerGlyph / 8;

    /// <summary>character codes of the font (Shift-JIS: 1 byte below 0x100, else lead byte × 256 + trail byte)</summary>
    public IEnumerable<int> Codes => _glyphOfCode.Keys;

    public bool HasCode(int code) => _glyphOfCode.ContainsKey(code);

    /// <summary>advance of the character in pixels, the gap to the next character included</summary>
    public int AdvanceOf(int code) => _data[WidthEntry(GlyphOf(code)) + 2];

    /// <summary>the character as drawn: <see cref="Glyph.Width"/> = its advance, <see cref="Glyph.Height"/> = cell height</summary>
    public Glyph ReadGlyph(int code)
    {
        var index = GlyphOf(code);
        var width = WidthEntry(index);
        var left = (sbyte)_data[width];
        var glyph = new Glyph(_data[width + 2], CellHeight);
        for (var y = 0; y < CellHeight; y++)
        {
            for (var x = 0; x < CellWidth; x++)
            {
                if (ReadPixel(index, x, y) != 0)
                {
                    glyph[left + x, y] = true;
                }
            }
        }

        return glyph;
    }

    /// <summary>
    /// Replaces the bitmap of the character and sets its widths: advance = <see cref="Glyph.Width"/>, left = first
    /// column with a pixel. The spare bits of the cell are kept.
    /// </summary>
    public void WriteGlyph(int code, Glyph glyph)
    {
        if (glyph.Height > CellHeight || glyph.Width > CellWidth)
        {
            throw new ArgumentException($"the glyph is {glyph.Width} × {glyph.Height} px, the font cells are {CellWidth} × {CellHeight}");
        }

        var columns = Enumerable.Range(0, glyph.Width).Where(x => Enumerable.Range(0, glyph.Height).Any(y => glyph[x, y])).ToList();
        var left = columns.Count == 0 ? 0 : columns.Min();
        var glyphWidth = columns.Count == 0 ? 0 : columns.Max() - left + 1;

        var index = GlyphOf(code);
        var max = (1 << _bitsPerPixel) - 1;
        for (var y = 0; y < CellHeight; y++)
        {
            for (var x = 0; x < CellWidth; x++)
            {
                WritePixel(index, x, y, glyph[left + x, y] ? max : 0);
            }
        }

        var width = WidthEntry(index);
        _data[width] = (byte)left;
        _data[width + 1] = (byte)glyphWidth;
        _data[width + 2] = (byte)glyph.Width;
    }

    /// <summary>bytes kept in the spare bits of the glyph cells, starting with glyph 0; the game does not draw them</summary>
    public byte[] ReadSpareBytes(int count)
    {
        var bytes = new byte[Math.Min(count, SpareByteCapacity)];
        for (var bit = 0; bit < bytes.Length * 8; bit++)
        {
            if (ReadSpareBit(bit))
            {
                bytes[bit / 8] |= (byte)(0x80 >> bit % 8);
            }
        }

        return bytes;
    }

    public void WriteSpareBytes(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length > SpareByteCapacity)
        {
            throw new InvalidOperationException($"{bytes.Length} bytes do not fit into the {SpareByteCapacity} spare bytes of the font");
        }

        for (var bit = 0; bit < bytes.Length * 8; bit++)
        {
            WriteSpareBit(bit, (bytes[bit / 8] & (0x80 >> bit % 8)) != 0);
        }
    }

    private int GlyphOf(int code) => _glyphOfCode.TryGetValue(code, out var index)
        ? index
        : throw new KeyNotFoundException($"the font has no character 0x{code:X}");

    private int WidthEntry(int glyph)
    {
        foreach (var (first, last, entries) in _widths)
        {
            if (glyph >= first && glyph <= last)
            {
                return entries + (glyph - first) * 3;
            }
        }

        throw new KeyNotFoundException($"the font has no width for glyph {glyph}");
    }

    private void ReadCharacterMap(int cmap)
    {
        int first = BitConverter.ToUInt16(_data, cmap + 8), last = BitConverter.ToUInt16(_data, cmap + 10);
        var type = BitConverter.ToUInt16(_data, cmap + 12);
        var entries = cmap + 20;
        switch (type)
        {
            case 0: // direct: consecutive glyphs
                var start = BitConverter.ToUInt16(_data, entries);
                for (var code = first; code <= last; code++)
                {
                    _glyphOfCode[code] = start + code - first;
                }

                break;
            case 1: // table: one glyph per code, 0xFFFF = none
                for (var code = first; code <= last; code++)
                {
                    var glyph = BitConverter.ToUInt16(_data, entries + (code - first) * 2);
                    if (glyph != 0xFFFF)
                    {
                        _glyphOfCode[code] = glyph;
                    }
                }

                break;
            case 2: // scan: count, then (code, glyph) pairs
                var count = BitConverter.ToUInt16(_data, entries);
                for (var i = 0; i < count; i++)
                {
                    _glyphOfCode[BitConverter.ToUInt16(_data, entries + 2 + i * 4)] = BitConverter.ToUInt16(_data, entries + 4 + i * 4);
                }

                break;
            default:
                throw new InvalidDataException($"unknown character map type {type}");
        }
    }

    // pixels are stored row by row from the most significant bit on
    private int ReadPixel(int glyph, int x, int y)
    {
        var bit = (y * CellWidth + x) * _bitsPerPixel;
        return _data[_glyphs + glyph * _cellSize + bit / 8] >> (8 - _bitsPerPixel - bit % 8) & (1 << _bitsPerPixel) - 1;
    }

    private void WritePixel(int glyph, int x, int y, int value)
    {
        var bit = (y * CellWidth + x) * _bitsPerPixel;
        var position = _glyphs + glyph * _cellSize + bit / 8;
        var shift = 8 - _bitsPerPixel - bit % 8;
        var mask = (1 << _bitsPerPixel) - 1 << shift;
        _data[position] = (byte)(_data[position] & ~mask | value << shift & mask);
    }

    private (int Position, int Mask) SpareBit(int bit)
    {
        var cellBit = CellWidth * CellHeight * _bitsPerPixel + bit % SpareBitsPerGlyph;
        return (_glyphs + bit / SpareBitsPerGlyph * _cellSize + cellBit / 8, 0x80 >> cellBit % 8);
    }

    private bool ReadSpareBit(int bit)
    {
        var (position, mask) = SpareBit(bit);
        return (_data[position] & mask) != 0;
    }

    private void WriteSpareBit(int bit, bool value)
    {
        var (position, mask) = SpareBit(bit);
        _data[position] = (byte)(value ? _data[position] | mask : _data[position] & ~mask);
    }

    public override string ToString() => $"NFTR {CellWidth}×{CellHeight}, {GlyphCount} glyphs, encoding {Encoding}";
}