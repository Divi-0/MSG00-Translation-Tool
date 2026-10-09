
namespace MSG00.Translation.Application.Features.Graphics;

public interface IWriter
{
    /// <summary>a copy of the archive with the tiles and map of the background</summary>
    byte[] Write(byte[] narc, Background background);

    /// <summary>a copy of the archive with the tiles of the sprites</summary>
    byte[] Write(byte[] narc, SpriteSet sprites);
}

/// <summary>
/// Writes tiles (NCGR) and map (NSCR) in place: both keep their size, so no other file of the archive moves.
/// </summary>
internal sealed class Writer : IWriter
{
    public byte[] Write(byte[] narc, Background background)
    {
        var entries = Narc.Read(narc).ToDictionary(x => x.Name.ToUpperInvariant());
        var tiles = NitroFile.Tiles(narc, entries[$"{background.Name}.NCGR".ToUpperInvariant()]);
        var map = NitroFile.Map(narc, entries[$"{background.Name}.NSCR".ToUpperInvariant()]);

        if (background.Tiles.Length != tiles.DataLength || background.Map.Length * 2 != map.DataLength)
        {
            throw new InvalidOperationException($"{background.Name}: tiles and map must keep their size");
        }

        var result = narc.ToArray();
        background.Tiles.CopyTo(result, tiles.DataOffset);
        for (var i = 0; i < background.Map.Length; i++)
        {
            BitConverter.GetBytes(background.Map[i]).CopyTo(result, map.DataOffset + i * 2);
        }

        return result;
    }

    public byte[] Write(byte[] narc, SpriteSet sprites)
    {
        var entries = Narc.Read(narc).ToDictionary(x => x.Name.ToUpperInvariant());
        var tiles = NitroFile.Tiles(narc, entries[$"{sprites.Name}.NCGR".ToUpperInvariant()]);
        if (sprites.Tiles.Length != tiles.DataLength)
        {
            throw new InvalidOperationException($"{sprites.Name}: tiles must keep their size");
        }

        var result = narc.ToArray();
        sprites.Tiles.CopyTo(result, tiles.DataOffset);
        return result;
    }
}