using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.Table;

/// <summary>
/// A csvb table database (e.g. App/OG_MapMission.csvb with OG_MAP, OG_MISSION, ... or the 2D/*.csvb Gunpla files
/// with one BLOCK1 table), see docs/table/README.md.
/// Only texts are editable; the writer keeps all other bytes of <see cref="Bytes"/>.
/// </summary>
public class TableCsvb : CsvbFile
{
    public TableCsvb() : this(CsvbFileType.OG_TABLE)
    {
    }

    /// <param name="type"><see cref="CsvbFileType.OG_TABLE"/> or <see cref="CsvbFileType.BLOCK"/></param>
    public TableCsvb(CsvbFileType type) : base(type)
    {
    }

    /// <summary>the original file</summary>
    public required byte[] Bytes { get; init; }

    public required IReadOnlyList<CsvbTable> Tables { get; init; }

    /// <summary>cells of text columns (column type <see cref="CsvbTable.TextColumnType"/>) that point to a text</summary>
    public required IReadOnlyList<TableText> Texts { get; init; }
}

/// <param name="Start">file offset of the first row</param>
public sealed record CsvbTable(string Name, int Rows, int RowSize, IReadOnlyList<int> ColumnTypes, int Start)
{
    public const int IntColumnType = 0x02;
    public const int TextColumnType = 0x07;
    public const int IdColumnType = 0x09;

    public int CellOffset(int row, int column) => Start + row * RowSize + column * 4;
}

public sealed class TableText
{
    public required string Table { get; init; }

    public required int Row { get; init; }

    public required int Column { get; init; }

    /// <summary>file offset of the 4-byte cell holding the text offset</summary>
    public required int CellOffset { get; init; }

    /// <summary>offset in the text table (relative to OffsetToEndOfFullHeader)</summary>
    public required int TextOffset { get; init; }

    public required string Original { get; init; }

    public required string Text { get; set; }

    public bool IsChanged => Text != Original;
}