using MSG00.Translation.Application.Features.Font;
using System.Text;

namespace MSG00.Translation.Application.Features.Csvb.Table;

public static class TableCells
{
    private static readonly Encoding ShiftJis = GameEncoding.Instance;

    /// <summary>text of a cell that holds a text offset, e.g. the id column (type 0x09) used as row label</summary>
    public static string ReadText(TableCsvb tableCsvb, CsvbTable table, int row, int column)
    {
        var textOffset = BitConverter.ToInt32(tableCsvb.Bytes, table.CellOffset(row, column));
        return textOffset == 0 ? string.Empty : ReadText(tableCsvb.Bytes, tableCsvb.CsvbHeader.OffsetToEndOfFullHeader + textOffset);
    }

    /// <summary>NUL-terminated Shift-JIS text at a file offset</summary>
    public static string ReadText(byte[] bytes, int offset)
    {
        var end = Array.IndexOf(bytes, (byte)0, offset);
        return ShiftJis.GetString(bytes, offset, end - offset);
    }
}