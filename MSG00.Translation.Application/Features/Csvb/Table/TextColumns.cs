namespace MSG00.Translation.Application.Features.Csvb.Table;

/// <param name="IdColumn">column whose text identifies the row (e.g. the mission id), shown as row label</param>
/// <param name="MaxBytesPerLine">visible width measured in the game, null when not measured yet</param>
/// <param name="MaxLines">lines shown in the game, null when not measured yet</param>
public sealed record TextColumn(string Table, int Column, string Label, int? IdColumn, int? MaxBytesPerLine, int? MaxLines = null);

/// <summary>
/// Text columns that may be translated. A fixed list: other text columns hold file names (e.g. OG_CHAPTER "C1_001")
/// that must not change.
/// </summary>
public static class TextColumns
{
    public static IReadOnlyList<TextColumn> All { get; } =
    [
        // field select and mission select: about 24 characters are visible before the NEW badge
        new("OG_MAP", 0, "Field name", null, 24, 1),
        new("OG_MISSION", 6, "Mission name", 0, 24, 1),
        // mission select, top screen "ミッション 内容": 3 lines like the conversation text box
        new("OG_MISSION", 7, "Mission description", 0, 38, 3),
        // 2D/*.csvb Gunpla files (ETC_*, FG_*, HCM_*, HG_*, Gunpla_*): column 1 = Gunpla Navi page or picture caption,
        // columns 2..4 = image names (shown as row label, must not change). Gunpla Navi in DeSmuME: 37 half-width
        // characters fit, the 38th is cut off; 6 lines = longest original page
        new("BLOCK1", 1, "Text", 2, 37, 6),
        // 2D/requirement.csvb: a single text column (in the Gunpla files column 0 is always empty). A changed text did
        // not show up on MISSION SELECT, the file looks unused

        new("BLOCK1", 0, "Requirement", null, 38, 4),
    ];

    public static TextColumn? Find(string table, int column) => All.FirstOrDefault(x => x.Table == table && x.Column == column);
}