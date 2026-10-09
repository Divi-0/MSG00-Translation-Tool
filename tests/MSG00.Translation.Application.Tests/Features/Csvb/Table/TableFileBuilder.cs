using System.Text;
using MSG00.Translation.Application.Features.Csvb.Table;
using MSG00.Translation.Application.Features.Csvb.Read;
using TableReader = MSG00.Translation.Application.Features.Csvb.Table.Reader;
using TableWriter = MSG00.Translation.Application.Features.Csvb.Table.Writer;

namespace MSG00.Translation.Application.Tests.Features.Csvb.Table;

/// <summary>
/// Builds a small table csvb like App/OG_MapMission.csvb: table descriptors, rows, column types, text table,
/// extra section and a data area at the end.
/// </summary>
internal static class TableFileBuilder
{
    /// <summary>data after the extra section; it contains a value that looks like a text offset and must stay unchanged</summary>
    public static readonly byte[] DataArea = [4, 0, 0, 0, 1, 2, 3, 4, 0x80, 0, 0, 0];

    private static readonly Encoding ShiftJis = Encoding.GetEncoding("Shift-JIS");

    /// <summary>
    /// OG_MAP: 3 rows (text, int); the third has no text.
    /// OG_MISSION: 2 rows (id, text, text); the second mission name shares its text with the first.
    /// </summary>
    public static byte[] Build()
    {
        var texts = new TextTable();
        var island = texts.Add("マーシャル諸島某島");
        var space = texts.Add("宇宙");
        var id1 = texts.Add("EVM_AEU001");
        var id2 = texts.Add("EVM_AEU002");
        var name = texts.Add("新型ＭＳ披露襲撃");
        var description = texts.Add("ＡＥＵが新型ＭＳ発表会を行う");

        return Assemble(
        [
            ("OG_MAP", [CsvbTable.TextColumnType, CsvbTable.IntColumnType], [[island, 193], [space, 92], [0, 5]]),
            ("OG_MISSION", [CsvbTable.IdColumnType, CsvbTable.TextColumnType, CsvbTable.TextColumnType], [[id1, name, description], [id2, name, 0]]),
        ], texts.Bytes, [0, 0, 0, 0, 2, 2, 10, 0], DataArea);
    }

    /// <summary>
    /// A Gunpla file like 2D/FG_EXIA.csvb: one BLOCK1 table with 5 text columns {empty, text, image, image, image},
    /// no extra section and no data area.
    /// </summary>
    public static byte[] BuildBlock()
    {
        var texts = new TextTable();
        var page = texts.Add(BlockPage);
        var front = texts.Add(BlockCaption);
        var fg = texts.Add("FG");
        var image = texts.Add("FG_01_01");

        var text = CsvbTable.TextColumnType;
        return Assemble(
        [
            ("BLOCK1", [text, text, text, text, text], [[0, page, fg, fg, fg], [0, front, image, image, image]]),
        ], texts.Bytes, [], []);
    }

    public const string BlockPage = "低価格で誰でも簡単に組み立てが可能。\n初めてガンプラを作るのに最適な、ＦＧ";
    public const string BlockCaption = "　　ＦＲＯＮＴ　ＶＩＥＷ　（前面）";

    private sealed class TextTable
    {
        public List<byte> Bytes { get; } = [0, 0, 0, 0];

        public int Add(string text)
        {
            var offset = Bytes.Count;
            var bytes = ShiftJis.GetBytes(text);
            Bytes.AddRange(bytes);
            Bytes.AddRange(new byte[4 - bytes.Length % 4]);
            return offset;
        }
    }

    private static byte[] Assemble((string Name, int[] Types, int[][] Rows)[] tables, List<byte> texts, byte[] extra, byte[] dataArea)
    {
        var descriptorsEnd = 0x20 + tables.Length * 40;
        var file = new List<byte>();
        file.AddRange("CSVBv4.3"u8.ToArray());
        file.AddRange(BitConverter.GetBytes(0x7A));
        file.AddRange(new byte[20]);

        var rowStart = descriptorsEnd;
        var columnTypeIndex = 0;
        foreach (var (tableName, types, rows) in tables)
        {
            var nameBytes = new byte[16];
            Encoding.ASCII.GetBytes(tableName).CopyTo(nameBytes, 0);
            file.AddRange(nameBytes);
            foreach (var value in new[] { 0, rows.Length, types.Length * 4, types.Length, columnTypeIndex, rowStart })
            {
                file.AddRange(BitConverter.GetBytes(value));
            }

            rowStart += rows.Length * types.Length * 4;
            columnTypeIndex += types.Length * 4;
        }

        foreach (var value in tables.SelectMany(x => x.Rows).SelectMany(x => x))
        {
            file.AddRange(BitConverter.GetBytes(value));
        }

        var endOfPointerTable = file.Count;
        foreach (var type in tables.SelectMany(x => x.Types))
        {
            file.AddRange(BitConverter.GetBytes(type));
        }

        var endOfFullHeader = file.Count;
        file.AddRange(texts);
        var endOfTextTable = file.Count;
        file.AddRange(extra);
        var endOfExtraTable = file.Count;
        file.AddRange(dataArea);

        var result = file.ToArray();
        BitConverter.GetBytes(endOfPointerTable).CopyTo(result, 0x0C);
        BitConverter.GetBytes(endOfFullHeader).CopyTo(result, 0x10);
        BitConverter.GetBytes(endOfTextTable).CopyTo(result, 0x14);
        BitConverter.GetBytes(endOfExtraTable).CopyTo(result, 0x18);
        return result;
    }

    public static async Task<TableCsvb> ReadAsync(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        var csvbFile = await new CsvbReader().ReadAsync(stream, TestContext.Current.CancellationToken);
        return await new TableReader().ReadFileAsync(csvbFile, stream);
    }

    public static async Task<byte[]> WriteAsync(TableCsvb tableCsvb)
    {
        using var stream = new MemoryStream();
        await new TableWriter().WriteFileAsync(stream, tableCsvb);
        return stream.ToArray();
    }
}