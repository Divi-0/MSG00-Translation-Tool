using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.CutIn;

/// <summary>
/// Cut-in lines (Cut/CUTIN.fcmd): many small tables (SFS0_001, MES_TGT01, ...) of {slot, command} rows; command 0x34
/// shows a line. See docs/cutin/README.md. Only the lines are editable; the writer keeps all other bytes.
/// </summary>
public class CutInCsvb : CsvbFile
{
    public CutInCsvb() : base(CsvbFileType.CUT_IN)
    {
    }

    /// <summary>the original file</summary>
    public required byte[] Bytes { get; init; }

    public required IReadOnlyList<CutInLine> Lines { get; init; }
}

public sealed class CutInLine
{
    /// <summary>name of the cut-in table, e.g. SFS0_006 or MES_TGT01</summary>
    public required string Table { get; init; }

    /// <summary>slot offset relative to the data area</summary>
    public required int SlotOffset { get; init; }

    public required int TextOffset { get; init; }

    /// <summary>slot word 1, probably the display time in 1/4096 frames</summary>
    public required int DisplayTime { get; init; }

    public required string Original { get; init; }

    public required string Text { get; set; }
}