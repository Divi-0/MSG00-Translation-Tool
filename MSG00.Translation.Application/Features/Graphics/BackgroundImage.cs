
namespace MSG00.Translation.Application.Features.Graphics;

/// <summary>
/// Converts a background to an RGBA image and back. Color index 0 is transparent (the game shows the backdrop).
/// Importing keeps the palette: every pixel gets the nearest palette color, identical and mirrored tiles are shared.
/// </summary>
public static class BackgroundImage
{
    private const int Tile = 8;
    private const ushort TileIndexMask = 0x3FF;
    private const ushort HorizontalFlip = 1 << 10;
    private const ushort VerticalFlip = 1 << 11;

    public static RgbaImage Render(Background background)
    {
        var pixels = new byte[background.Width * background.Height * 4];
        var columns = background.Width / Tile;
        for (var cell = 0; cell < background.Map.Length && cell / columns * Tile < background.Height; cell++)
        {
            var entry = background.Map[cell];
            var tile = entry & TileIndexMask;
            if (tile >= background.TileCount)
            {
                continue;
            }

            var indices = TileIndices(background, tile);
            var palette = background.BitsPerPixel == 4 ? (entry >> 12) * 16 : 0;
            for (var y = 0; y < Tile; y++)
            {
                for (var x = 0; x < Tile; x++)
                {
                    var index = indices[((entry & VerticalFlip) != 0 ? 7 - y : y) * Tile + ((entry & HorizontalFlip) != 0 ? 7 - x : x)];
                    if (index == 0 || palette + index >= background.Colors.Count)
                    {
                        continue;
                    }

                    var color = background.Colors[palette + index];
                    var p = ((cell / columns * Tile + y) * background.Width + cell % columns * Tile + x) * 4;
                    pixels[p] = color.R;
                    pixels[p + 1] = color.G;
                    pixels[p + 2] = color.B;
                    pixels[p + 3] = 255;
                }
            }
        }

        return new RgbaImage(background.Width, background.Height, pixels);
    }

    /// <summary>new tiles and map for the image; the background itself is not changed</summary>
    /// <exception cref="InvalidOperationException">wrong size or more different tiles than the NCGR can hold</exception>
    public static (byte[] Tiles, ushort[] Map) Import(Background background, RgbaImage image)
    {
        if (image.Width != background.Width || image.Height != background.Height)
        {
            throw new InvalidOperationException($"The image must be {background.Width} × {background.Height} pixels, it is {image.Width} × {image.Height}.");
        }

        var columns = background.Width / Tile;
        var palettes = background.BitsPerPixel == 4 ? Math.Max(1, background.Colors.Count / 16) : 1;
        var tiles = new List<byte[]>();
        var known = new Dictionary<string, int>();
        var map = new ushort[background.Map.Length];
        var nearest = new Dictionary<(int Rgb, int Palette), (int Index, int Error)>();

        for (var cell = 0; cell < map.Length; cell++)
        {
            if (cell / columns * Tile >= background.Height)
            {
                map[cell] = background.Map[cell];
                continue;
            }

            // 16-color backgrounds: the palette with the smallest error; on a tie the one the cell had before
            var previous = background.BitsPerPixel == 4 ? background.Map[cell] >> 12 : 0;
            var best = (Palette: previous, Indices: Array.Empty<byte>(), Error: long.MaxValue);
            foreach (var palette in Enumerable.Range(0, palettes).OrderBy(x => x != previous))
            {
                var (indices, error) = Quantize(background, image, cell % columns * Tile, cell / columns * Tile, palette, nearest);
                if (error < best.Error)
                {
                    best = (palette, indices, error);
                }
            }

            var (tile, flips) = Share(best.Indices, tiles, known);
            if (tile > TileIndexMask)
            {
                throw new InvalidOperationException("The image needs more than 1024 different tiles.");
            }

            map[cell] = (ushort)(tile | flips | best.Palette << 12);
        }

        if (tiles.Count > background.TileCount)
        {
            throw new InvalidOperationException($"The image needs {tiles.Count} different 8×8 tiles, the background has room for {background.TileCount}. Use fewer different details (e.g. no smooth gradients behind new text).");
        }

        var data = new byte[background.Tiles.Length];
        for (var t = 0; t < tiles.Count; t++)
        {
            Pack(tiles[t], background.BitsPerPixel).CopyTo(data, t * background.TileSize);
        }

        return (data, map);
    }

    private static byte[] TileIndices(Background background, int tile)
    {
        var data = background.Tiles.AsSpan(tile * background.TileSize, background.TileSize);
        return background.BitsPerPixel == 8
            ? data.ToArray()
            : data.ToArray().SelectMany(b => new[] { (byte)(b & 0x0F), (byte)(b >> 4) }).ToArray();
    }

    private static (byte[] Indices, long Error) Quantize(Background background, RgbaImage image, int left, int top, int palette, Dictionary<(int, int), (int Index, int Error)> nearest)
    {
        var indices = new byte[Tile * Tile];
        long error = 0;
        for (var y = 0; y < Tile; y++)
        {
            for (var x = 0; x < Tile; x++)
            {
                var p = ((top + y) * image.Width + left + x) * 4;
                if (image.Pixels[p + 3] < 128)
                {
                    continue;
                }

                var rgb = image.Pixels[p] << 16 | image.Pixels[p + 1] << 8 | image.Pixels[p + 2];
                if (!nearest.TryGetValue((rgb, palette), out var match))
                {
                    match = Nearest(background, palette, image.Pixels[p], image.Pixels[p + 1], image.Pixels[p + 2]);
                    nearest[(rgb, palette)] = match;
                }

                indices[y * Tile + x] = (byte)match.Index;
                error += match.Error;
            }
        }

        return (indices, error);
    }

    /// <summary>nearest opaque color (index 1..) of the palette</summary>
    private static (int Index, int Error) Nearest(Background background, int palette, int r, int g, int b)
    {
        var size = background.BitsPerPixel == 4 ? 16 : 256;
        var first = palette * size;
        var best = (Index: 1, Error: int.MaxValue);
        for (var i = 1; i < size && first + i < background.Colors.Count; i++)
        {
            var c = background.Colors[first + i];
            var error = (c.R - r) * (c.R - r) + (c.G - g) * (c.G - g) + (c.B - b) * (c.B - b);
            if (error < best.Error)
            {
                best = (i, error);
            }
        }

        return best;
    }

    /// <summary>index of an equal tile (also mirrored) or of the newly added one, with the flip bits</summary>
    private static (int Tile, ushort Flips) Share(byte[] indices, List<byte[]> tiles, Dictionary<string, int> known)
    {
        foreach (var flips in new[] { (ushort)0, HorizontalFlip, VerticalFlip, (ushort)(HorizontalFlip | VerticalFlip) })
        {
            if (known.TryGetValue(Convert.ToBase64String(Flip(indices, flips)), out var existing))
            {
                return (existing, flips);
            }
        }

        known[Convert.ToBase64String(indices)] = tiles.Count;
        tiles.Add(indices);
        return (tiles.Count - 1, 0);
    }

    private static byte[] Flip(byte[] indices, ushort flips)
    {
        var flipped = new byte[indices.Length];
        for (var y = 0; y < Tile; y++)
        {
            for (var x = 0; x < Tile; x++)
            {
                flipped[y * Tile + x] = indices[((flips & VerticalFlip) != 0 ? 7 - y : y) * Tile + ((flips & HorizontalFlip) != 0 ? 7 - x : x)];
            }
        }

        return flipped;
    }

    private static byte[] Pack(byte[] indices, int bitsPerPixel) => bitsPerPixel == 8
        ? indices
        : Enumerable.Range(0, indices.Length / 2).Select(i => (byte)(indices[i * 2] | indices[i * 2 + 1] << 4)).ToArray();
}