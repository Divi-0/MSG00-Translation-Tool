using System.Text;
using MSG00.Translation.Application.Features.Csvb.CutIn;
using MSG00.Translation.Application.Features.Csvb.Read;
using CutInReader = MSG00.Translation.Application.Features.Csvb.CutIn.Reader;
using CutInWriter = MSG00.Translation.Application.Features.Csvb.CutIn.Writer;

namespace MSG00.Translation.Application.Tests.Features.Csvb.CutIn;

/// <summary>
/// Builds a small Cut/CUTIN.fcmd: two cut-in tables of {slot, command} rows, column types, text table, extra section
/// and the data area with the slots.
/// </summary>
internal static class CutInFileBuilder
{
    public const string ExiaLine = "ミッション終了。　エクシア、帰還する";
    public const string TargetLine = "目標施設を破壊してください";

    private static readonly Encoding ShiftJis = Encoding.GetEncoding("Shift-JIS");

    public static byte[] Build()
    {
        var texts = new List<byte> { 0, 0, 0, 0 };
        int Text(string text)
        {
            var offset = texts.Count;
            var bytes = ShiftJis.GetBytes(text);
            texts.AddRange(bytes);
            texts.AddRange(new byte[4 - bytes.Length % 4]);
            return offset;
        }

        var exia = Text(ExiaLine);
        var target = Text(TargetLine);

        var tables = new (string Name, (int Type, int[] Words)[] Commands)[]
        {
            ("SFS0_006", [(0x33, [1, 2]), (0x34, [122880, 655360, exia])]),
            ("MES_TGT01", [(0x34, [0, 983040, target])]),
        };

        var file = new List<byte>();
        file.AddRange("CSVBv4.3"u8.ToArray());
        file.AddRange(BitConverter.GetBytes(0xAA));
        file.AddRange(new byte[20]);

        var rowStart = 0x20 + tables.Length * 40;
        var columnTypeIndex = 0;
        foreach (var (name, commands) in tables)
        {
            var nameBytes = new byte[16];
            Encoding.ASCII.GetBytes(name).CopyTo(nameBytes, 0);
            file.AddRange(nameBytes);
            foreach (var value in new[] { 1, commands.Length, 8, 2, columnTypeIndex, rowStart })
            {
                file.AddRange(BitConverter.GetBytes(value));
            }

            rowStart += commands.Length * 8;
            columnTypeIndex += 8;
        }

        var slot = 0;
        foreach (var (type, words) in tables.SelectMany(x => x.Commands))
        {
            file.AddRange(BitConverter.GetBytes(slot));
            file.AddRange(BitConverter.GetBytes(type));
            slot += words.Length * 4;
        }

        var endOfPointerTable = file.Count;
        foreach (var _ in Enumerable.Range(0, tables.Length * 2))
        {
            file.AddRange(BitConverter.GetBytes(2));
        }

        var endOfFullHeader = file.Count;
        file.AddRange(texts);
        var endOfTextTable = file.Count;
        file.AddRange(new byte[] { 0, 0, 0, 0, 2, 2, 0, 0 });
        var endOfExtraTable = file.Count;
        foreach (var word in tables.SelectMany(x => x.Commands).SelectMany(x => x.Words))
        {
            file.AddRange(BitConverter.GetBytes(word));
        }

        var result = file.ToArray();
        BitConverter.GetBytes(endOfPointerTable).CopyTo(result, 0x0C);
        BitConverter.GetBytes(endOfFullHeader).CopyTo(result, 0x10);
        BitConverter.GetBytes(endOfTextTable).CopyTo(result, 0x14);
        BitConverter.GetBytes(endOfExtraTable).CopyTo(result, 0x18);
        return result;
    }

    public static async Task<CutInCsvb> ReadAsync(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var csvbFile = await new CsvbReader().ReadAsync(stream, TestContext.Current.CancellationToken);
        return await new CutInReader().ReadFileAsync(csvbFile, stream);
    }

    public static async Task<byte[]> WriteAsync(CutInCsvb cutInCsvb)
    {
        using var stream = new MemoryStream();
        await new CutInWriter().WriteFileAsync(stream, cutInCsvb);
        return stream.ToArray();
    }
}