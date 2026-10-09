using MSG00.Translation.Application.Features.Csvb.Mapi;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll;

/// <summary>
/// Credits (2D/StaffRoll*.csvb, MAPI type 0x0D): a script of image commands and texts. Each credit page has two
/// texts, the left column (job titles, character names) and the right column (names), aligned line by line with
/// full-width spaces. Only the texts are editable; the writer keeps all other bytes. See docs/staffroll/README.md.
/// </summary>
public class StaffRollTextCsvb : MapiCsvb
{
    /// <summary>the original file</summary>
    public required byte[] Bytes { get; init; }

    public required IReadOnlyList<StaffRollText> Texts { get; init; }
}

public sealed class StaffRollText
{
    /// <summary>slot offset relative to the data area</summary>
    public required int SlotOffset { get; init; }

    public required int TextOffset { get; init; }

    /// <summary>second slot word, constant per page (probably the display time)</summary>
    public required int Parameter { get; init; }

    public required string Original { get; init; }

    public required string Text { get; set; }
}