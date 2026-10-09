namespace MSG00.Translation.Application.Features.Graphics;

/// <summary>
/// Sprites inside a NARC archive (e.g. 2D/Briefing.narc → dst/Mission_Obj01): cells (NCER) built from OAM entries
/// that point into the tiles (NCGR), colors from the NCLR of the same name. Animation frames are cells that share tiles,
/// so changing the tiles of one cell changes all frames. See docs/graphics/README.md.
/// </summary>
public sealed class SpriteSet
{
    /// <summary>name inside the archive without extension, e.g. "dst/Mission_Obj01"</summary>
    public required string Name { get; init; }

    /// <summary>bits per pixel of the NCGR; each OAM has its own flag, see <see cref="Oam.Is8Bit"/></summary>
    public required int BitsPerPixel { get; init; }

    public required IReadOnlyList<Rgb> Colors { get; init; }

    /// <summary>tile data of the NCGR, changed in place</summary>
    public required byte[] Tiles { get; set; }

    public required IReadOnlyList<SpriteCell> Cells { get; init; }
}

public sealed class SpriteCell
{
    public required int Index { get; init; }

    public required IReadOnlyList<Oam> Oams { get; init; }

    public int Left => Oams.Min(x => x.X);

    public int Top => Oams.Min(x => x.Y);

    public int Width => Oams.Max(x => x.X + x.Width) - Left;

    public int Height => Oams.Max(x => x.Y + x.Height) - Top;
}

/// <param name="TileOffset">byte offset of the first tile in <see cref="SpriteSet.Tiles"/>; the tiles follow row by row (1D mapping)</param>
/// <param name="Palette">palette number of the OAM (16 colors each, 256 for 8-bit OAMs with extended palettes)</param>
public sealed record Oam(int X, int Y, int Width, int Height, int TileOffset, int Palette, bool HorizontalFlip, bool VerticalFlip, bool Is8Bit);