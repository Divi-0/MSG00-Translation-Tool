using System.Text;

namespace MSG00.Translation.Application.Features.Graphics;

/// <param name="Offset">offset of the file data inside the archive</param>
public sealed record NarcEntry(string Name, int Offset, int Length);

/// <summary>
/// Reads the file table of a NARC archive (chunks BTAF = offsets, BTNF = names, GMIF = data).
/// Files are changed in place, so the archive is never rebuilt.
/// </summary>
public static class Narc
{
    public static IReadOnlyList<NarcEntry> Read(byte[] narc)
    {
        if (!narc.AsSpan(0, 4).SequenceEqual("NARC"u8))
        {
            throw new InvalidDataException("not a NARC archive");
        }

        var chunks = new Dictionary<string, int>();
        for (int offset = BitConverter.ToUInt16(narc, 0x0C); offset + 8 <= narc.Length; offset += BitConverter.ToInt32(narc, offset + 4))
        {
            chunks[Encoding.ASCII.GetString(narc, offset, 4)] = offset;
        }

        var btaf = chunks["BTAF"];
        var data = chunks["GMIF"] + 8;
        var names = ReadNames(narc, chunks["BTNF"] + 8);
        var count = BitConverter.ToUInt16(narc, btaf + 8);

        return Enumerable.Range(0, count).Select(i =>
        {
            var start = BitConverter.ToInt32(narc, btaf + 12 + i * 8);
            var end = BitConverter.ToInt32(narc, btaf + 16 + i * 8);
            return new NarcEntry(i < names.Count ? names[i] : i.ToString(), data + start, end - start);
        }).ToList();
    }

    /// <summary>file names in file id order; archives without names return none</summary>
    private static List<string> ReadNames(byte[] narc, int fnt)
    {
        var names = new List<string>();

        void Walk(int offset, string prefix)
        {
            while (narc[offset] is var length && length != 0)
            {
                var name = Encoding.ASCII.GetString(narc, offset + 1, length & 0x7F);
                offset += 1 + (length & 0x7F);
                if ((length & 0x80) == 0)
                {
                    names.Add(prefix + name);
                    continue;
                }

                var directory = BitConverter.ToUInt16(narc, offset) & 0xFFF;
                offset += 2;
                Walk(fnt + BitConverter.ToInt32(narc, fnt + directory * 8), $"{prefix}{name}/");
            }
        }

        Walk(fnt + BitConverter.ToInt32(narc, fnt), string.Empty);
        return names;
    }
}