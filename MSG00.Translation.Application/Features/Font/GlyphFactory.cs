using System.Globalization;
using System.Text;

namespace MSG00.Translation.Application.Features.Font;

/// <summary>
/// First drafts of extra characters in the style of the game font: accented letters are the letter of the font plus
/// the accent (capitals and tall letters are squeezed from 10 to 8 rows to make room), a few others are drawn here.
/// Characters it can not build start empty and are drawn in the font editor.
/// </summary>
public static class GlyphFactory
{
    /// <summary>advance of the 1-byte characters: 5 px letter + 1 px gap</summary>
    public const int Width = 6;

    /// <summary>rows 0–9 = capitals, 3–9 = lowercase, 10 = descender</summary>
    public const int Height = 11;

    private const int DescenderRow = 10;

    /// <summary>accents above, drawn centered over the letter; 1-row accents get a free row towards the letter</summary>
    private static readonly Dictionary<char, string[]> MarksAbove = new()
    {
        ['̀'] = ["#..", ".#."], // grave
        ['́'] = ["..#", ".#."], // acute
        ['̂'] = [".#.", "#.#"], // circumflex
        ['̃'] = [".##.#", "#.##."], // tilde
        ['̄'] = ["###"], // macron
        ['̆'] = ["#...#", ".###."], // breve
        ['̇'] = ["#"], // dot above
        ['̈'] = ["#.#"], // diaeresis
        ['̊'] = ["##", "##"], // ring
        ['̋'] = [".#.#", "#.#."], // double acute
        ['̌'] = ["#.#", ".#."], // caron
    };

    /// <summary>marks below the letter, in the descender row: x = center or right edge of the letter</summary>
    private static readonly Dictionary<char, bool> MarksBelowAtRight = new()
    {
        ['̦'] = false, // comma below
        ['̧'] = false, // cedilla
        ['̨'] = true, // ogonek
    };

    /// <summary>characters without a letter + accent decomposition, 6 × 11 (column 0 is the gap, as in the font)</summary>
    private static readonly Dictionary<char, string[]> Drawn = new()
    {
        ['ß'] = ["..##..", ".#..#.", ".#..#.", ".#.#..", ".#..#.", ".#...#", ".#...#", ".#...#", ".#..#.", ".#.#.."],
        ['Œ'] = ["..####", ".#.#..", ".#.#..", ".#.#..", ".#.###", ".#.#..", ".#.#..", ".#.#..", ".#.#..", "..####"],
        ['œ'] = ["", "", "", "..#.#.", ".#.#.#", ".#.#.#", ".#.###", ".#.#..", ".#.#.#", "..#.#."],
        ['Æ'] = ["..####", ".#.#..", ".#.#..", ".#.#..", ".#####", ".#.#..", ".#.#..", ".#.#..", ".#.#..", ".#.###"],
        ['æ'] = ["", "", "", "..#.#.", ".#.#.#", "...#.#", "..####", ".#.#..", ".#.#.#", "..#.#."],
        ['Ø'] = ["..####", ".#..##", ".#..##", ".#.#.#", ".#.#.#", ".#.#.#", ".#.#.#", ".##..#", ".##..#", ".####."],
        ['ø'] = ["", "", "", "..####", ".#..##", ".#.#.#", ".#.#.#", ".#.#.#", ".##..#", ".####."],
        ['Ł'] = [".#....", ".#....", ".#....", ".#.#..", ".##...", "##....", ".#....", ".#....", ".#....", ".#####"],
        ['ł'] = ["..#...", "..#...", "..#...", "..#...", "..##..", ".##...", "..#...", "..#...", "..#...", "..#..."],
        ['Đ'] = [".####.", ".#...#", ".#...#", ".#...#", "####.#", ".#...#", ".#...#", ".#...#", ".#...#", ".####."],
        ['đ'] = [".....#", "...###", ".....#", "..####", ".#...#", ".#...#", ".#...#", ".#...#", ".#...#", "..####"],
        ['«'] = ["", "", "", "", "...#.#", "..#.#.", ".#.#..", "..#.#.", "...#.#"],
        ['»'] = ["", "", "", "", ".#.#..", "..#.#.", "...#.#", "..#.#.", ".#.#.."],
        ['€'] = ["..###.", ".#...#", ".#....", "####..", ".#....", "####..", ".#....", ".#....", ".#...#", "..###."],
    };

    /// <summary>the character upside down: ¡ from !, ¿ from ?</summary>
    private static readonly Dictionary<char, char> Rotated = new() { ['¡'] = '!', ['¿'] = '?' };

    /// <param name="fontGlyph">the glyph of an ASCII character in the font, null when the font has none</param>
    /// <returns>a 6 × 11 draft, null when the character can not be built</returns>
    public static Glyph? Create(char character, Func<char, Glyph?> fontGlyph)
    {
        if (Drawn.TryGetValue(character, out var rows))
        {
            return Glyph.FromRows(rows, Width, Height);
        }

        if (Rotated.TryGetValue(character, out var source))
        {
            return fontGlyph(source) is { } glyph ? Rotate(Fit(glyph)) : null;
        }

        var decomposed = character.ToString().Normalize(NormalizationForm.FormD);
        if (decomposed.Length < 2 || decomposed[0] >= 0x80 || fontGlyph(decomposed[0]) is not { } baseGlyph)
        {
            return null;
        }

        var marks = decomposed[1..];
        if (marks.Any(m => CharUnicodeInfo.GetUnicodeCategory(m) != UnicodeCategory.NonSpacingMark)
            || marks.Count(MarksAbove.ContainsKey) > 1
            || marks.Any(m => !MarksAbove.ContainsKey(m) && !MarksBelowAtRight.ContainsKey(m)))
        {
            return null;
        }

        var letter = Fit(baseGlyph);
        if (decomposed[0] is 'i' or 'j' && marks.Any(MarksAbove.ContainsKey))
        {
            // dotless, the accent takes the place of the dot
            ClearRows(letter, 0, 2);
        }

        if (marks.FirstOrDefault(MarksAbove.ContainsKey) is var above && above != default)
        {
            letter = AddMarkAbove(letter, MarksAbove[above]);
        }

        foreach (var below in marks.Where(MarksBelowAtRight.ContainsKey))
        {
            var (left, right) = Columns(letter);
            letter[MarksBelowAtRight[below] ? right : (left + right + 1) / 2, DescenderRow] = true;
        }

        return letter;
    }

    private static Glyph AddMarkAbove(Glyph letter, string[] mark)
    {
        var isTall = Enumerable.Range(0, 3).Any(y => RowHasPixels(letter, y));
        var result = isTall ? Squeeze(letter) : letter.Clone();

        // short letters start at row 3: the accent ends in row 1 and row 2 stays free;
        // squeezed ones start at row 2: a 1-row accent goes to row 0, a 2-row accent fills rows 0–1
        var top = isTall ? 0 : 2 - mark.Length;
        var (left, right) = Columns(letter);
        var center = (left + right + 1) / 2;
        var start = center - (mark[0].Length - 1) / 2;
        for (var y = 0; y < mark.Length; y++)
        {
            for (var x = 0; x < mark[y].Length; x++)
            {
                if (mark[y][x] == '#')
                {
                    result[Math.Clamp(start + x, 0, Width - 1), top + y] = true;
                }
            }
        }

        return result;
    }

    /// <summary>rows 0–9 shrunk to rows 2–9 by dropping a row of the two longest runs of equal rows; the descender row stays</summary>
    private static Glyph Squeeze(Glyph letter)
    {
        var rows = Enumerable.Range(0, DescenderRow).Select(y => Enumerable.Range(0, Width).Select(x => letter[x, y]).ToArray()).ToList();
        for (var removed = 0; removed < 2; removed++)
        {
            var (start, length) = (0, 0);
            for (var y = 0; y < rows.Count;)
            {
                var end = y;
                while (end + 1 < rows.Count && rows[end + 1].SequenceEqual(rows[y]))
                {
                    end++;
                }

                // longest run; between equal runs the one closer to the middle
                var runLength = end - y + 1;
                if (runLength > length || runLength == length && Math.Abs(y + end - rows.Count) < Math.Abs(start * 2 + length - 1 - rows.Count))
                {
                    (start, length) = (y, runLength);
                }

                y = end + 1;
            }

            if (length > 1)
            {
                rows.RemoveAt(start + length / 2);
            }
            else
            {
                // no repeated rows: merge the two middle rows
                var middle = rows.Count / 2;
                rows[middle - 1] = rows[middle - 1].Zip(rows[middle], (a, b) => a || b).ToArray();
                rows.RemoveAt(middle);
            }
        }

        var result = new Glyph(Width, Height);
        for (var y = 0; y < rows.Count; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                result[x, y + 2] = rows[y][x];
            }
        }

        for (var x = 0; x < Width; x++)
        {
            result[x, DescenderRow] = letter[x, DescenderRow];
        }

        return result;
    }

    private static Glyph Rotate(Glyph glyph)
    {
        var (left, right) = Columns(glyph);
        var result = new Glyph(Width, Height);
        for (var y = 0; y < Height; y++)
        {
            for (var x = left; x <= right; x++)
            {
                result[left + right - x, Height - 1 - y] = glyph[x, y];
            }
        }

        return result;
    }

    /// <summary>the font glyph on a 6 × 11 canvas</summary>
    private static Glyph Fit(Glyph glyph)
    {
        var result = new Glyph(Width, Height);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                result[x, y] = glyph[x, y];
            }
        }

        return result;
    }

    private static (int Left, int Right) Columns(Glyph glyph)
    {
        var columns = Enumerable.Range(0, glyph.Width).Where(x => Enumerable.Range(0, glyph.Height).Any(y => glyph[x, y])).ToList();
        return columns.Count == 0 ? (1, Width - 1) : (columns.Min(), columns.Max());
    }

    private static bool RowHasPixels(Glyph glyph, int y) => Enumerable.Range(0, glyph.Width).Any(x => glyph[x, y]);

    private static void ClearRows(Glyph glyph, int from, int to)
    {
        for (var y = from; y <= to; y++)
        {
            for (var x = 0; x < glyph.Width; x++)
            {
                glyph[x, y] = false;
            }
        }
    }
}