namespace MSG00.Translation.Application.Features.Font;

/// <summary>
/// A character as the game draws it: <see cref="Width"/> = advance in pixels (6 for 1-byte characters, the gap to
/// the next character included), <see cref="Height"/> = line height (11). Pixels are on or off.
/// </summary>
public sealed class Glyph
{
    private readonly bool[] _pixels;

    public Glyph(int width, int height)
    {
        Width = width;
        Height = height;
        _pixels = new bool[width * height];
    }

    public int Width { get; }

    public int Height { get; }

    public bool this[int x, int y]
    {
        get => x >= 0 && x < Width && y >= 0 && y < Height && _pixels[y * Width + x];
        set
        {
            if (x >= 0 && x < Width && y >= 0 && y < Height)
            {
                _pixels[y * Width + x] = value;
            }
        }
    }

    public bool IsEmpty => !_pixels.Any(x => x);

    public Glyph Clone()
    {
        var clone = new Glyph(Width, Height);
        _pixels.CopyTo(clone._pixels, 0);
        return clone;
    }

    /// <summary>one string per row, '#' = pixel on, '.' = off</summary>
    public IReadOnlyList<string> ToRows() => Enumerable.Range(0, Height)
        .Select(y => new string(Enumerable.Range(0, Width).Select(x => this[x, y] ? '#' : '.').ToArray()))
        .ToList();

    /// <summary>reverse of <see cref="ToRows"/>; missing rows and columns stay off, extra ones are cut</summary>
    public static Glyph FromRows(IReadOnlyList<string> rows, int width, int height)
    {
        var glyph = new Glyph(width, height);
        for (var y = 0; y < Math.Min(rows.Count, height); y++)
        {
            for (var x = 0; x < Math.Min(rows[y].Length, width); x++)
            {
                glyph[x, y] = rows[y][x] == '#';
            }
        }

        return glyph;
    }

    public bool SamePixels(Glyph other) => Width == other.Width && Height == other.Height && _pixels.AsSpan().SequenceEqual(other._pixels);

    public override string ToString() => string.Join("\n", ToRows());
}