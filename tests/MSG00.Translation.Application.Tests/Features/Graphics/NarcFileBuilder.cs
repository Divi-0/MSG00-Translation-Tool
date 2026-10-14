using System.Text;

namespace MSG00.Translation.Application.Tests.Features.Graphics;

/// <summary>
/// Builds a small NARC like 2D/Briefing.narc: files in a "dst" folder with a background of 2 × 1 tiles
/// (NCLR + NCGR + NSCR) and a file that is not part of a background.
/// </summary>
internal static class NarcFileBuilder
{
    public const string Name = "dst/Test_BG";

    /// <summary>palette 0: transparent, red, green, blue; palette 1 (4 bpp only): transparent, white</summary>
    public static readonly (byte R, byte G, byte B)[] Colors = [(0, 0, 0), (255, 0, 0), (0, 255, 0), (0, 0, 255)];

    /// <summary>
    /// Map of 2 × 1 cells; tile 0 = red left half / green right half, tile 1 = blue; one spare tile.
    /// Cell 0 = tile 0, cell 1 = tile 0 mirrored horizontally.
    /// </summary>
    public static byte[] Build(int bitsPerPixel = 4, int spareTiles = 1)
    {
        var tile0 = Enumerable.Range(0, 64).Select(i => (byte)(i % 8 < 4 ? 1 : 2)).ToArray();
        var tile1 = Enumerable.Repeat((byte)3, 64).ToArray();
        var tiles = new[] { tile0, tile1 }.Concat(Enumerable.Range(0, spareTiles).Select(_ => new byte[64])).SelectMany(t => Pack(t, bitsPerPixel)).ToArray();
        ushort[] map = [0, 1 << 10];

        return Narc(
        [
            ("dst/Other.NANR", NitroFile("RNAN", "KNBA", new byte[8])),
            ($"{Name}.NCGR", Ncgr(tiles, bitsPerPixel)),
            ($"{Name}.NCLR", Nclr(bitsPerPixel)),
            ($"{Name}.NSCR", Nscr(16, 8, map)),
        ]);
    }

    public const string SpriteName = "dst/Test_OBJ";

    /// <summary>
    /// Sprites, 4 bpp, NCLR as <see cref="Colors"/> + 12 black: tile 0 = red, tile 1 = left half green / right half
    /// transparent, tile 2 = blue.
    /// Cell 0: OAM 8×8 tile 1 at (0, 0) in front of OAM 8×8 tile 0 at (0, 0) and OAM 8×8 tile 2 at (8, 0).
    /// Cell 1: OAM tile 0 alone (another animation frame showing the hidden tile).
    /// </summary>
    public static byte[] BuildSprites()
    {
        var red = Enumerable.Repeat((byte)1, 64).ToArray();
        var greenHalf = Enumerable.Range(0, 64).Select(i => (byte)(i % 8 < 4 ? 2 : 0)).ToArray();
        var blue = Enumerable.Repeat((byte)3, 64).ToArray();
        var tiles = new[] { red, greenHalf, blue }.SelectMany(t => Pack(t, 4)).ToArray();

        // OAM: attr0 = y (square shape, 16 colors), attr1 = x (size 8×8), attr2 = tile
        ushort[][] cells = [[0, 0, 1, 0, 0, 0, 0, 8, 2], [0, 0, 0]];
        var cellTable = new List<byte>();
        var oams = new List<byte>();
        foreach (var cell in cells)
        {
            cellTable.AddRange(BitConverter.GetBytes((ushort)(cell.Length / 3)));
            cellTable.AddRange(BitConverter.GetBytes((ushort)0));
            cellTable.AddRange(BitConverter.GetBytes(oams.Count));
            oams.AddRange(cell.SelectMany(BitConverter.GetBytes));
        }

        var section = new List<byte>();
        section.AddRange(BitConverter.GetBytes((ushort)cells.Length));
        section.AddRange(BitConverter.GetBytes((ushort)0)); // 8-byte cell entries
        section.AddRange(BitConverter.GetBytes(0x18));
        section.AddRange(BitConverter.GetBytes(0)); // mapping shift 0: tile number × 32 bytes
        section.AddRange(new byte[12]); // cell table starts 0x18 after the section header
        section.AddRange(cellTable);
        section.AddRange(oams);

        return Narc(
        [
            ($"{SpriteName}.NCER", NitroFile("RECN", "KBEC", section.ToArray())),
            ($"{SpriteName}.NCGR", Ncgr(tiles, 4)),
            ($"{SpriteName}.NCLR", Nclr(8)),
        ]);
    }

    private static byte[] Pack(byte[] indices, int bitsPerPixel) => bitsPerPixel == 8
        ? indices
        : Enumerable.Range(0, 32).Select(i => (byte)(indices[i * 2] | indices[i * 2 + 1] << 4)).ToArray();

    private static byte[] Nclr(int bitsPerPixel)
    {
        var colors = Colors.Concat(Enumerable.Repeat(((byte)0, (byte)0, (byte)0), 12)).ToList();
        if (bitsPerPixel == 4)
        {
            colors.Add((0, 0, 0));
            colors.Add((255, 255, 255));
            colors.AddRange(Enumerable.Repeat(((byte)0, (byte)0, (byte)0), 14));
        }

        var data = colors.SelectMany(c => BitConverter.GetBytes((ushort)(c.Item1 >> 3 | c.Item2 >> 3 << 5 | c.Item3 >> 3 << 10))).ToArray();
        var section = new List<byte>();
        section.AddRange(BitConverter.GetBytes(bitsPerPixel == 4 ? 3 : 4));
        section.AddRange(BitConverter.GetBytes(0));
        section.AddRange(BitConverter.GetBytes(data.Length));
        section.AddRange(BitConverter.GetBytes(0x10));
        section.AddRange(data);
        return NitroFile("RLCN", "TTLP", section.ToArray());
    }

    private static byte[] Ncgr(byte[] tiles, int bitsPerPixel)
    {
        var section = new List<byte>();
        section.AddRange(BitConverter.GetBytes((ushort)1));
        section.AddRange(BitConverter.GetBytes((ushort)(tiles.Length / (8 * bitsPerPixel))));
        section.AddRange(BitConverter.GetBytes(bitsPerPixel == 4 ? 3 : 4));
        section.AddRange(BitConverter.GetBytes(0));
        section.AddRange(BitConverter.GetBytes(0)); // tiled
        section.AddRange(BitConverter.GetBytes(tiles.Length));
        section.AddRange(BitConverter.GetBytes(0x18));
        section.AddRange(tiles);
        return NitroFile("RGCN", "RAHC", section.ToArray());
    }

    private static byte[] Nscr(int width, int height, ushort[] map)
    {
        var section = new List<byte>();
        section.AddRange(BitConverter.GetBytes((ushort)width));
        section.AddRange(BitConverter.GetBytes((ushort)height));
        section.AddRange(BitConverter.GetBytes(0));
        section.AddRange(BitConverter.GetBytes(map.Length * 2));
        section.AddRange(map.SelectMany(BitConverter.GetBytes));
        return NitroFile("RCSN", "NRCS", section.ToArray());
    }

    /// <summary>16-byte header (magic, BOM, version, size, header size 0x10, 1 section) + one section</summary>
    private static byte[] NitroFile(string magic, string sectionMagic, byte[] section)
    {
        var file = new List<byte>();
        file.AddRange(Encoding.ASCII.GetBytes(magic));
        file.AddRange([0xFF, 0xFE, 0x00, 0x01]);
        file.AddRange(BitConverter.GetBytes(16 + 8 + section.Length));
        file.AddRange(BitConverter.GetBytes((ushort)0x10));
        file.AddRange(BitConverter.GetBytes((ushort)1));
        file.AddRange(Encoding.ASCII.GetBytes(sectionMagic));
        file.AddRange(BitConverter.GetBytes(8 + section.Length));
        file.AddRange(section);
        return file.ToArray();
    }

    /// <summary>BTAF (offsets), BTNF (root folder with sub folder "dst"), GMIF (data, 4-byte aligned)</summary>
    internal static byte[] Narc((string Name, byte[] Data)[] files)
    {
        var btaf = new List<byte>();
        btaf.AddRange(BitConverter.GetBytes(files.Length));
        var gmif = new List<byte>();
        foreach (var (_, data) in files)
        {
            btaf.AddRange(BitConverter.GetBytes(gmif.Count));
            btaf.AddRange(BitConverter.GetBytes(gmif.Count + data.Length));
            gmif.AddRange(data);
            while (gmif.Count % 4 != 0)
            {
                gmif.Add(0xFF);
            }
        }

        // directory table: root (sub table offset, first file id, directory count) and "dst"
        var rootTable = new List<byte> { 0x83 };
        rootTable.AddRange("dst"u8.ToArray());
        rootTable.AddRange(BitConverter.GetBytes((ushort)0xF001));
        rootTable.Add(0);
        var dstTable = new List<byte>();
        foreach (var (name, _) in files)
        {
            var fileName = name.Split('/').Last();
            dstTable.Add((byte)fileName.Length);
            dstTable.AddRange(Encoding.ASCII.GetBytes(fileName));
        }

        dstTable.Add(0);
        var btnf = new List<byte>();
        btnf.AddRange(BitConverter.GetBytes(16));
        btnf.AddRange(BitConverter.GetBytes((ushort)0));
        btnf.AddRange(BitConverter.GetBytes((ushort)2));
        btnf.AddRange(BitConverter.GetBytes(16 + rootTable.Count));
        btnf.AddRange(BitConverter.GetBytes((ushort)0));
        btnf.AddRange(BitConverter.GetBytes((ushort)0xF000));
        btnf.AddRange(rootTable);
        btnf.AddRange(dstTable);
        while (btnf.Count % 4 != 0)
        {
            btnf.Add(0xFF);
        }

        var chunks = new[] { ("BTAF", btaf), ("BTNF", btnf), ("GMIF", gmif) }
            .SelectMany(x => Encoding.ASCII.GetBytes(x.Item1).Concat(BitConverter.GetBytes(8 + x.Item2.Count)).Concat(x.Item2))
            .ToArray();
        var narc = new List<byte>();
        narc.AddRange("NARC"u8.ToArray());
        narc.AddRange([0xFE, 0xFF, 0x00, 0x01]);
        narc.AddRange(BitConverter.GetBytes(16 + chunks.Length));
        narc.AddRange(BitConverter.GetBytes((ushort)0x10));
        narc.AddRange(BitConverter.GetBytes((ushort)3));
        narc.AddRange(chunks);
        return narc.ToArray();
    }
}