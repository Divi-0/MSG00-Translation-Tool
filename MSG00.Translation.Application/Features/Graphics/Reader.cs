
namespace MSG00.Translation.Application.Features.Graphics;

public interface IReader
{
    /// <summary>every NSCR of the archive together with the NCGR and NCLR of the same name</summary>
    IReadOnlyList<Background> ReadBackgrounds(byte[] narc);

    /// <summary>every NCER of the archive with the NCGR of the same name and the NCLR of the same name (or the only/first one)</summary>
    IReadOnlyList<SpriteSet> ReadSprites(byte[] narc);
}

/// <summary>
/// Reads backgrounds: NCLR section TTLP (BGR555 colors), NCGR section RAHC (tiles), NSCR section NRCS (map).
/// </summary>
internal sealed class Reader : IReader
{
    public IReadOnlyList<Background> ReadBackgrounds(byte[] narc)
    {
        var entries = Narc.Read(narc);
        var byName = entries.ToDictionary(x => x.Name.ToUpperInvariant());
        var backgrounds = new List<Background>();

        foreach (var screen in entries.Where(x => x.Name.EndsWith(".NSCR", StringComparison.OrdinalIgnoreCase)))
        {
            var name = screen.Name[..^5];
            if (!byName.TryGetValue($"{name}.NCGR".ToUpperInvariant(), out var tiles) || !byName.TryGetValue($"{name}.NCLR".ToUpperInvariant(), out var palette))
            {
                continue;
            }

            var tileSection = NitroFile.Tiles(narc, tiles);
            if (tileSection.IsLinear)
            {
                continue;
            }

            var mapSection = NitroFile.Map(narc, screen);
            backgrounds.Add(new Background
            {
                Name = name,
                Width = mapSection.Width,
                Height = mapSection.Height,
                BitsPerPixel = tileSection.BitsPerPixel,
                Colors = NitroFile.Colors(narc, palette),
                Tiles = narc.AsSpan(tileSection.DataOffset, tileSection.DataLength).ToArray(),
                Map = Enumerable.Range(0, mapSection.DataLength / 2).Select(i => BitConverter.ToUInt16(narc, mapSection.DataOffset + i * 2)).ToArray(),
            });
        }

        return backgrounds;
    }

    public IReadOnlyList<SpriteSet> ReadSprites(byte[] narc)
    {
        var entries = Narc.Read(narc);
        var byName = entries.ToDictionary(x => x.Name.ToUpperInvariant());
        var anyPalette = entries.FirstOrDefault(x => x.Name.EndsWith(".NCLR", StringComparison.OrdinalIgnoreCase));
        var sprites = new List<SpriteSet>();

        foreach (var cells in entries.Where(x => x.Name.EndsWith(".NCER", StringComparison.OrdinalIgnoreCase)))
        {
            var name = cells.Name[..^5];
            if (!byName.TryGetValue($"{name}.NCGR".ToUpperInvariant(), out var tiles))
            {
                continue;
            }

            var palette = byName.GetValueOrDefault($"{name}.NCLR".ToUpperInvariant()) ?? anyPalette;
            if (palette == null)
            {
                continue;
            }

            var tileSection = NitroFile.Tiles(narc, tiles);
            sprites.Add(new SpriteSet
            {
                Name = name,
                BitsPerPixel = tileSection.BitsPerPixel,
                Colors = NitroFile.Colors(narc, palette),
                Tiles = narc.AsSpan(tileSection.DataOffset, tileSection.DataLength).ToArray(),
                Cells = NitroFile.Cells(narc, cells),
            });
        }

        return sprites;
    }
}

/// <summary>offsets inside a NARC of the parts of a Nitro file (header size at 0x0C, then sections {magic, size, ...})</summary>
internal static class NitroFile
{
    public sealed record TileSection(int BitsPerPixel, bool IsLinear, int DataOffset, int DataLength);

    public sealed record MapSection(int Width, int Height, int DataOffset, int DataLength);

    public static IReadOnlyList<Rgb> Colors(byte[] narc, NarcEntry palette)
    {
        var section = Section(narc, palette, "TTLP"u8);
        var length = BitConverter.ToInt32(narc, section + 0x10);
        var data = section + 8 + BitConverter.ToInt32(narc, section + 0x14);
        length = Math.Min(length, palette.Offset + palette.Length - data);

        return Enumerable.Range(0, length / 2).Select(i =>
        {
            var color = BitConverter.ToUInt16(narc, data + i * 2);
            return new Rgb(To8Bit(color & 31), To8Bit(color >> 5 & 31), To8Bit(color >> 10 & 31));
        }).ToList();
    }

    public static TileSection Tiles(byte[] narc, NarcEntry tiles)
    {
        var section = Section(narc, tiles, "RAHC"u8);
        var bitsPerPixel = BitConverter.ToInt32(narc, section + 0x0C) == 3 ? 4 : 8;
        var isLinear = (BitConverter.ToInt32(narc, section + 0x14) & 0xFF) != 0;
        return new TileSection(bitsPerPixel, isLinear, section + 8 + BitConverter.ToInt32(narc, section + 0x1C), BitConverter.ToInt32(narc, section + 0x18));
    }

    public static MapSection Map(byte[] narc, NarcEntry screen)
    {
        var section = Section(narc, screen, "NRCS"u8);
        return new MapSection(BitConverter.ToUInt16(narc, section + 8), BitConverter.ToUInt16(narc, section + 10), section + 0x14, BitConverter.ToInt32(narc, section + 0x10));
    }

    /// <summary>
    /// NCER section KBEC: cell count, bank type (1 = 16-byte cell entries with bounds), data offset, mapping shift.
    /// Cell entry: OAM count, attributes, offset into the OAM list; OAM = 3 × u16 (GBA/DS OAM attributes 0–2).
    /// Cells without OAMs are left out.
    /// </summary>
    public static IReadOnlyList<SpriteCell> Cells(byte[] narc, NarcEntry cells)
    {
        var section = Section(narc, cells, "KBEC"u8);
        var count = BitConverter.ToUInt16(narc, section + 8);
        var entrySize = BitConverter.ToUInt16(narc, section + 10) == 1 ? 16 : 8;
        var table = section + 8 + BitConverter.ToInt32(narc, section + 12);
        var mapping = BitConverter.ToInt32(narc, section + 16) & 0xFF;
        var oamList = table + count * entrySize;

        var result = new List<SpriteCell>();
        for (var i = 0; i < count; i++)
        {
            var oamCount = BitConverter.ToUInt16(narc, table + i * entrySize);
            var first = oamList + BitConverter.ToInt32(narc, table + i * entrySize + 4);
            var oams = Enumerable.Range(0, oamCount).Select(k => Oam(narc, first + k * 6, mapping)).ToList();
            if (oams.Count > 0)
            {
                result.Add(new SpriteCell { Index = i, Oams = oams });
            }
        }

        return result;
    }

    private static readonly (int Width, int Height)[][] OamSizes =
    [
        [(8, 8), (16, 16), (32, 32), (64, 64)],
        [(16, 8), (32, 8), (32, 16), (64, 32)],
        [(8, 16), (8, 32), (16, 32), (32, 64)],
    ];

    private static Oam Oam(byte[] narc, int offset, int mapping)
    {
        var a0 = BitConverter.ToUInt16(narc, offset);
        var a1 = BitConverter.ToUInt16(narc, offset + 2);
        var a2 = BitConverter.ToUInt16(narc, offset + 4);
        var y = (sbyte)(a0 & 0xFF);
        var x = (a1 & 0x1FF) >= 256 ? (a1 & 0x1FF) - 512 : a1 & 0x1FF;
        var (width, height) = OamSizes[Math.Min(a0 >> 14, 2)][a1 >> 14];
        var affine = (a0 & 0x100) != 0;
        return new Oam(x, y, width, height, (a2 & 0x3FF) << mapping << 5, a2 >> 12,
            !affine && (a1 & 0x1000) != 0, !affine && (a1 & 0x2000) != 0, (a0 & 0x2000) != 0);
    }

    private static int Section(byte[] narc, NarcEntry file, ReadOnlySpan<byte> magic)
    {
        var end = file.Offset + file.Length;
        for (var offset = file.Offset + BitConverter.ToUInt16(narc, file.Offset + 0x0C); offset + 8 <= end; offset += BitConverter.ToInt32(narc, offset + 4))
        {
            if (narc.AsSpan(offset, 4).SequenceEqual(magic))
            {
                return offset;
            }
        }

        throw new InvalidDataException($"{file.Name} has no {System.Text.Encoding.ASCII.GetString(magic)} section");
    }

    /// <summary>5-bit DS color channel to 8 bit</summary>
    private static byte To8Bit(int value) => (byte)(value << 3 | value >> 2);
}