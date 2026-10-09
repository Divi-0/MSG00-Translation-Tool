using System.Text;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.Table;

public interface IReader
{
    Task<TableCsvb> ReadFileAsync(CsvbFile csvbFile, Stream stream);
}

/// <summary>
/// Reads the table descriptors at 0x20 (16-byte name + flag, rows, row size, columns, column type index, start),
/// the column types (between OffsetToEndOfPointerTable and OffsetToEndOfFullHeader) and the texts of text columns.
/// </summary>
internal sealed class Reader : IReader
{
    private const int FirstDescriptor = 0x20;
    private const int DescriptorSize = 40;
    private const int NameLength = 16;

    public async Task<TableCsvb> ReadFileAsync(CsvbFile csvbFile, Stream stream)
    {
        var bytes = new byte[stream.Length];
        stream.Position = 0;
        await stream.ReadExactlyAsync(bytes).ConfigureAwait(false);

        var header = csvbFile.CsvbHeader;
        var tables = ReadTables(bytes, header.OffsetToEndOfPointerTable);
        var texts = new List<TableText>();
        foreach (var table in tables)
        {
            for (var column = 0; column < table.ColumnTypes.Count; column++)
            {
                if (table.ColumnTypes[column] != CsvbTable.TextColumnType)
                {
                    continue;
                }

                for (var row = 0; row < table.Rows; row++)
                {
                    var cellOffset = table.CellOffset(row, column);
                    var textOffset = BitConverter.ToInt32(bytes, cellOffset);
                    if (textOffset == 0)
                    {
                        continue;
                    }

                    var text = TableCells.ReadText(bytes, header.OffsetToEndOfFullHeader + textOffset);
                    texts.Add(new TableText { Table = table.Name, Row = row, Column = column, CellOffset = cellOffset, TextOffset = textOffset, Original = text, Text = text });
                }
            }
        }

        return new TableCsvb(csvbFile.Type) { CsvbHeader = header, Bytes = bytes, Tables = tables, Texts = texts };
    }

    /// <summary>the descriptors end where the rows of the first table start</summary>
    private static List<CsvbTable> ReadTables(byte[] bytes, int columnTypesStart)
    {
        var tables = new List<CsvbTable>();
        var firstRows = int.MaxValue;
        for (var p = FirstDescriptor; p < firstRows; p += DescriptorSize)
        {
            var name = Encoding.ASCII.GetString(bytes, p, NameLength).TrimEnd('\0');
            var rows = BitConverter.ToInt32(bytes, p + 20);
            var rowSize = BitConverter.ToInt32(bytes, p + 24);
            var columns = BitConverter.ToInt32(bytes, p + 28);
            var columnTypeIndex = BitConverter.ToInt32(bytes, p + 32);
            var start = BitConverter.ToInt32(bytes, p + 36);
            firstRows = Math.Min(firstRows, start);
            var columnTypes = Enumerable.Range(0, columns).Select(c => BitConverter.ToInt32(bytes, columnTypesStart + columnTypeIndex + c * 4)).ToList();
            tables.Add(new CsvbTable(name, rows, rowSize, columnTypes, start));
        }

        return tables;
    }
}