using System.Text;
using MSG00.Translation.Application.Tests.Features.Graphics;

namespace MSG00.Translation.Application.Tests.Features.Font;

/// <summary>
/// Builds a small LC_font_12.NFTR like the game font: 11 × 11 cells, 1 bpp, 16 bytes per cell, Shift-JIS.
/// Character maps: scan (type 2) for the ASCII glyphs of <see cref="Ascii"/>, table (type 1) for the half-width
/// katakana 0xA1–0xDF, direct (type 0) for ぁ あ ぃ (0x829F–0x82A1, 12 px wide).
/// </summary>
internal static class NitroFontBuilder
{
    public const string FontName = "dst/LC_font_12.NFTR";

    /// <summary>ASCII glyphs as drawn in the game font; '.' / '#' rows, column 0 = left edge of the bitmap</summary>
    public static readonly Dictionary<char, (int Left, string[] Rows)> Ascii = new()
    {
        ['A'] = (1, ["..#", "..#", "..#", ".#.#", ".#.#", ".#.#", ".#.#", "#####", "#...#", "#...#"]),
        ['O'] = (1, [".###", "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###"]),
        ['a'] = (1, ["", "", "", ".###", "#...#", "....#", ".####", "#...#", "#...#", ".####"]),
        ['c'] = (1, ["", "", "", ".###", "#...#", "#", "#", "#", "#...#", ".###"]),
        ['i'] = (3, ["", "#", "", "#", "#", "#", "#", "#", "#", "#"]),
        ['!'] = (3, ["#", "#", "#", "#", "#", "#", "#", "#", "", "#"]),
        ['?'] = (1, [".###", "#...#", "#...#", "....#", "...#", "..#", "..#", "..#", "", "..#"]),
    };

    public static readonly int[] TwoByteCodes = [0x829F, 0x82A0, 0x82A1];

    public static byte[] Build()
    {
        var ascii = Ascii.Keys.Order().ToList();
        var glyphs = new List<(int Left, int Width, int Advance, string[] Rows)>();
        glyphs.AddRange(ascii.Select(c => (Ascii[c].Left, Ascii[c].Rows.Max(r => r.Length), 6, Ascii[c].Rows)));

        // katakana slots: a vertical bar at column (code % 5) so each one is different
        for (var code = 0xA1; code <= 0xDF; code++)
        {
            var bar = new string('.', code % 5) + "#";
            glyphs.Add((0, code % 5 + 1, 6, Enumerable.Repeat(bar, 10).ToArray()));
        }

        foreach (var _ in TwoByteCodes)
        {
            glyphs.Add((0, 11, 12, Enumerable.Repeat("###########", 11).ToArray()));
        }

        var cglp = new List<byte> { 11, 11 };
        cglp.AddRange(BitConverter.GetBytes((ushort)16));
        cglp.AddRange([10, 12, 1, 0]); // baseline, max width, bpp, flags
        foreach (var (_, _, _, rows) in glyphs)
        {
            var cell = new byte[16];
            for (var y = 0; y < rows.Length; y++)
            {
                for (var x = 0; x < rows[y].Length; x++)
                {
                    if (rows[y][x] == '#')
                    {
                        var bit = y * 11 + x;
                        cell[bit / 8] |= (byte)(0x80 >> bit % 8);
                    }
                }
            }

            cglp.AddRange(cell);
        }

        var cwdh = new List<byte>();
        cwdh.AddRange(BitConverter.GetBytes((ushort)0));
        cwdh.AddRange(BitConverter.GetBytes((ushort)(glyphs.Count - 1)));
        cwdh.AddRange(BitConverter.GetBytes(0));
        foreach (var (left, width, advance, _) in glyphs)
        {
            cwdh.AddRange([(byte)left, (byte)width, (byte)advance]);
        }

        var katakanaGlyph = ascii.Count;
        var twoByteGlyph = katakanaGlyph + (0xDF - 0xA1 + 1);
        byte[][] cmaps =
        [
            CharacterMap(0, 0xFFFF, 2, [(ushort)ascii.Count, .. ascii.SelectMany((c, i) => new[] { (ushort)c, (ushort)i })]),
            CharacterMap(0xA1, 0xDF, 1, Enumerable.Range(0, 0xDF - 0xA1 + 1).Select(i => (ushort)(katakanaGlyph + i)).ToArray()),
            CharacterMap(TwoByteCodes[0], TwoByteCodes[^1], 0, [(ushort)twoByteGlyph]),
        ];

        // header 0x10, FINF (8 + 20), CGLP, CWDH, CMAPs; block offsets point behind the 8-byte block header
        var finfStart = 0x10;
        var cglpStart = finfStart + 28;
        var cwdhStart = Align(cglpStart + 8 + cglp.Count);
        var cmapStarts = new List<int> { Align(cwdhStart + 8 + cwdh.Count) };
        foreach (var cmap in cmaps.SkipLast(1))
        {
            cmapStarts.Add(cmapStarts[^1] + 8 + cmap.Length);
        }

        var finf = new List<byte> { 0, 11 };
        finf.AddRange(BitConverter.GetBytes((ushort)0));
        finf.AddRange([0, 11, 11, 2]); // default widths, encoding Shift-JIS
        finf.AddRange(BitConverter.GetBytes(cglpStart + 8));
        finf.AddRange(BitConverter.GetBytes(cwdhStart + 8));
        finf.AddRange(BitConverter.GetBytes(cmapStarts[0] + 8));

        var file = new List<byte>();
        file.AddRange(Block("FNIF", finf));
        file.AddRange(Block("PLGC", cglp));
        Pad(file, cwdhStart - 0x10);
        file.AddRange(Block("HDWC", cwdh));
        Pad(file, cmapStarts[0] - 0x10);
        for (var i = 0; i < cmaps.Length; i++)
        {
            var cmap = cmaps[i].ToArray();
            BitConverter.GetBytes(i + 1 < cmaps.Length ? cmapStarts[i + 1] + 8 : 0).CopyTo(cmap, 8);
            file.AddRange(Block("PAMC", cmap));
        }

        var header = new List<byte>();
        header.AddRange("RTFN"u8.ToArray());
        header.AddRange([0xFF, 0xFE, 0x01, 0x01]);
        header.AddRange(BitConverter.GetBytes(0x10 + file.Count));
        header.AddRange(BitConverter.GetBytes((ushort)0x10));
        header.AddRange(BitConverter.GetBytes((ushort)(2 + cmaps.Length + 1)));
        return header.Concat(file).ToArray();
    }

    /// <summary>a NARC with the font and another file, like 2D/Common.narc</summary>
    public static byte[] BuildNarc() => NarcFileBuilder.Narc([("dst/Other.NCLR", new byte[32]), (FontName, Build())]);

    /// <summary>a NARC without a font</summary>
    public static byte[] BuildNarcWithoutFont() => NarcFileBuilder.Narc([("dst/Other.NCLR", new byte[32])]);

    private static byte[] CharacterMap(int first, int last, ushort type, ushort[] entries)
    {
        var cmap = new List<byte>();
        cmap.AddRange(BitConverter.GetBytes((ushort)first));
        cmap.AddRange(BitConverter.GetBytes((ushort)last));
        cmap.AddRange(BitConverter.GetBytes(type));
        cmap.AddRange(BitConverter.GetBytes((ushort)0));
        cmap.AddRange(BitConverter.GetBytes(0)); // next, patched in Build
        cmap.AddRange(entries.SelectMany(BitConverter.GetBytes));
        Pad(cmap, Align(cmap.Count));
        return cmap.ToArray();
    }

    private static byte[] Block(string magic, IReadOnlyCollection<byte> data) =>
        Encoding.ASCII.GetBytes(magic).Concat(BitConverter.GetBytes(8 + data.Count)).Concat(data).ToArray();

    private static int Align(int value) => (value + 3) / 4 * 4;

    private static void Pad(List<byte> bytes, int length)
    {
        while (bytes.Count < length)
        {
            bytes.Add(0);
        }
    }
}