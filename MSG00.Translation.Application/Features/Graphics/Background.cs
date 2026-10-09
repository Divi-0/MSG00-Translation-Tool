namespace MSG00.Translation.Application.Features.Graphics;

/// <summary>
/// A background screen inside a NARC archive (e.g. 2D/Briefing.narc → dst/Briefing2_2B_BG): palette (NCLR),
/// tiles (NCGR) and screen map (NSCR) with the same name. See docs/graphics/README.md.
/// </summary>
public sealed class Background
{
    /// <summary>name inside the archive without extension, e.g. "dst/Briefing2_2B_BG"</summary>
    public required string Name { get; init; }

    /// <summary>size of the screen map in pixels (usually 256 × 256, the DS shows the top 256 × 192)</summary>
    public required int Width { get; init; }

    public required int Height { get; init; }

    /// <summary>4 (16 colors per tile, 16 palettes) or 8 (256 colors)</summary>
    public required int BitsPerPixel { get; init; }

    public required IReadOnlyList<Rgb> Colors { get; init; }

    /// <summary>tile data of the NCGR; its size is fixed, an import must fit into it</summary>
    public required byte[] Tiles { get; set; }

    /// <summary>NSCR entries: tile index (bits 0–9), horizontal flip (10), vertical flip (11), palette (12–15)</summary>
    public required ushort[] Map { get; set; }

    public int TileSize => 8 * BitsPerPixel;

    public int TileCount => Tiles.Length / TileSize;
}

public readonly record struct Rgb(byte R, byte G, byte B);

/// <summary>RGBA, 4 bytes per pixel, row by row</summary>
public sealed record RgbaImage(int Width, int Height, byte[] Pixels);