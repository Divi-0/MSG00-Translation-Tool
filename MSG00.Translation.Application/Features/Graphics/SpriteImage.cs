
namespace MSG00.Translation.Application.Features.Graphics;

/// <summary>
/// Converts a sprite cell to an RGBA image (its bounding box) and back. The first OAM of a cell is drawn on top.
/// Importing writes only the changed pixels, each into the tiles of the front-most OAM covering it (nearest color of
/// its palette, transparent = index 0). The cell keeps its shape and size; other cells using the same tiles
/// (animation frames) change too. Text usually is an OAM of its own in front of shared plate tiles, so new text must
/// stay inside the area of the old text's OAMs.
/// </summary>
public static class SpriteImage
{
    private const int Tile = 8;

    public static RgbaImage Render(SpriteSet sprites, SpriteCell cell)
    {
        var image = new RgbaImage(cell.Width, cell.Height, new byte[cell.Width * cell.Height * 4]);
        foreach (var oam in cell.Oams.Reverse())
        {
            for (var y = 0; y < oam.Height; y++)
            {
                for (var x = 0; x < oam.Width; x++)
                {
                    var (offset, nibble) = PixelOffset(sprites, oam, x, y);
                    if (offset >= sprites.Tiles.Length)
                    {
                        continue;
                    }

                    var index = oam.Is8Bit ? sprites.Tiles[offset] : nibble ? sprites.Tiles[offset] >> 4 : sprites.Tiles[offset] & 0x0F;
                    var color = PaletteStart(sprites, oam) + index;
                    if (index == 0 || color >= sprites.Colors.Count)
                    {
                        continue;
                    }

                    var p = ((oam.Y - cell.Top + y) * image.Width + oam.X - cell.Left + x) * 4;
                    image.Pixels[p] = sprites.Colors[color].R;
                    image.Pixels[p + 1] = sprites.Colors[color].G;
                    image.Pixels[p + 2] = sprites.Colors[color].B;
                    image.Pixels[p + 3] = 255;
                }
            }
        }

        return image;
    }

    /// <summary>new tile data with the image written into the tiles of the cell; the sprite set itself is not changed</summary>
    /// <exception cref="InvalidOperationException">the image does not have the size of the cell</exception>
    public static byte[] Import(SpriteSet sprites, SpriteCell cell, RgbaImage image)
    {
        if (image.Width != cell.Width || image.Height != cell.Height)
        {
            throw new InvalidOperationException($"The image must be {cell.Width} × {cell.Height} pixels, it is {image.Width} × {image.Height}.");
        }

        // only changed pixels are written, each into the front-most OAM covering it; OAMs behind are only touched
        // to clear a pixel made transparent, and never when their tiles are used several times in the cell (plates,
        // frames). Unchanged pixels keep their tiles exactly.
        var before = Render(sprites, cell);
        var sharedInCell = cell.Oams.Select(o => cell.Oams.Count(x => x.TileOffset == o.TileOffset) > 1).ToArray();
        var front = FrontOams(cell, sharedInCell);
        var tiles = sprites.Tiles.ToArray();
        var nearest = new Dictionary<(int Rgb, int Palette), byte>();
        for (var i = 0; i < cell.Oams.Count; i++)
        {
            var oam = cell.Oams[i];
            for (var y = 0; y < oam.Height; y++)
            {
                for (var x = 0; x < oam.Width; x++)
                {
                    var (offset, nibble) = PixelOffset(sprites, oam, x, y);
                    var pixel = (oam.Y - cell.Top + y) * image.Width + oam.X - cell.Left + x;
                    var p = pixel * 4;
                    var transparent = image.Pixels[p + 3] < 128;
                    // a pixel made transparent is also cleared in OAMs behind with tiles of their own (otherwise they
                    // would show through); plates whose tiles are used several times in the cell are left alone
                    var target = front[pixel] == i || transparent && !sharedInCell[i];
                    if (offset >= tiles.Length || !target || SamePixel(before.Pixels, image.Pixels, p))
                    {
                        continue;
                    }

                    var index = transparent ? (byte)0 : Nearest(sprites, oam, image.Pixels[p], image.Pixels[p + 1], image.Pixels[p + 2], nearest);
                    tiles[offset] = oam.Is8Bit ? index
                        : nibble ? (byte)(tiles[offset] & 0x0F | index << 4) : (byte)(tiles[offset] & 0xF0 | index);
                }
            }
        }

        return tiles;
    }

    /// <summary>
    /// per pixel of the cell: the OAM a changed pixel is written to, -1 for none. That is the front-most OAM covering
    /// it with tiles of its own (e.g. a text part behind a shared selection frame); when all covering OAMs use shared
    /// tiles, the front-most one.
    /// </summary>
    private static int[] FrontOams(SpriteCell cell, bool[] sharedInCell)
    {
        var front = Enumerable.Repeat(-1, cell.Width * cell.Height).ToArray();
        for (var i = cell.Oams.Count - 1; i >= 0; i--)
        {
            var oam = cell.Oams[i];
            for (var y = 0; y < oam.Height; y++)
            {
                for (var x = 0; x < oam.Width; x++)
                {
                    var pixel = (oam.Y - cell.Top + y) * cell.Width + oam.X - cell.Left + x;
                    // walking back to front: a later (more front) OAM wins unless it is shared and an own one covers the pixel
                    if (front[pixel] < 0 || !sharedInCell[i] || sharedInCell[front[pixel]])
                    {
                        front[pixel] = i;
                    }
                }
            }
        }

        return front;
    }

    /// <summary>equal, or both transparent</summary>
    private static bool SamePixel(byte[] a, byte[] b, int p) =>
        a[p + 3] < 128 && b[p + 3] < 128 || a[p + 3] >= 128 && b[p + 3] >= 128 && a[p] == b[p] && a[p + 1] == b[p + 1] && a[p + 2] == b[p + 2];

    /// <summary>byte of a pixel of an OAM (x, y inside the OAM as shown, flips applied); for 4 bpp also which nibble</summary>
    private static (int Offset, bool HighNibble) PixelOffset(SpriteSet sprites, Oam oam, int x, int y)
    {
        var tx = oam.HorizontalFlip ? oam.Width - 1 - x : x;
        var ty = oam.VerticalFlip ? oam.Height - 1 - y : y;
        var tile = ty / Tile * (oam.Width / Tile) + tx / Tile;
        var pixel = ty % Tile * Tile + tx % Tile;
        return oam.Is8Bit
            ? (oam.TileOffset + tile * 64 + pixel, false)
            : (oam.TileOffset + tile * 32 + pixel / 2, pixel % 2 == 1);
    }

    /// <summary>16 colors per palette; 8-bit OAMs use 256 (extended palettes when the NCLR has more than 256 colors)</summary>
    private static int PaletteStart(SpriteSet sprites, Oam oam) => oam.Is8Bit
        ? sprites.Colors.Count > 256 ? oam.Palette * 256 : 0
        : oam.Palette * 16;

    private static byte Nearest(SpriteSet sprites, Oam oam, int r, int g, int b, Dictionary<(int, int), byte> cache)
    {
        var start = PaletteStart(sprites, oam);
        var key = (r << 16 | g << 8 | b, start);
        if (cache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var size = oam.Is8Bit ? 256 : 16;
        var best = (Index: 1, Error: int.MaxValue);
        for (var i = 1; i < size && start + i < sprites.Colors.Count; i++)
        {
            var c = sprites.Colors[start + i];
            var error = (c.R - r) * (c.R - r) + (c.G - g) * (c.G - g) + (c.B - b) * (c.B - b);
            if (error < best.Error)
            {
                best = (i, error);
            }
        }

        cache[key] = (byte)best.Index;
        return (byte)best.Index;
    }
}