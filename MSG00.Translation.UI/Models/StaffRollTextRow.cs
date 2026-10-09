using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.UI.Models;

/// <summary>
/// One staff roll text: the left column (titles) or right column (names) of a credit page.
/// </summary>
public sealed class StaffRollTextRow : NotifyBase
{
    /// <summary>longest original lines are 21 full-width characters (indentation included)</summary>
    public const int MaxBytesPerLine = 42;

    private static readonly Encoding ShiftJisEncoding = Encoding.GetEncoding("Shift-JIS");

    public StaffRollTextRow(StaffRollText text, int index)
    {
        StaffRollText = text;
        Index = index;
    }

    public StaffRollText StaffRollText { get; }

    /// <summary>position in the file, key for import</summary>
    public int Index { get; }

    /// <summary>texts come in pairs per page: left column, then right column</summary>
    public string Label => $"Page {Index / 2 + 1} · {(Index % 2 == 0 ? "left column (titles)" : "right column (names)")}";

    public string Original => StaffRollText.Original;

    public string Text
    {
        get => StaffRollText.Text;
        set
        {
            if (StaffRollText.Text == value)
            {
                return;
            }

            StaffRollText.Text = value;
            RaisePropertyChanged(nameof(Text));
            RaisePropertyChanged(nameof(Warning));
            RaisePropertyChanged(nameof(HasWarning));
        }
    }

    public string Warning
    {
        get
        {
            var lines = Text.Split('\n');
            var originalLines = Original.Split('\n').Length;
            var warnings = new List<string>();
            if (lines.Length != originalLines)
            {
                warnings.Add($"{lines.Length} lines, the original has {originalLines}; keep the line count so both columns stay aligned");
            }

            warnings.AddRange(lines
                .Select((line, index) => (index, bytes: ShiftJisEncoding.GetByteCount(line)))
                .Where(x => x.bytes > MaxBytesPerLine)
                .Select(x => $"Line {x.index + 1} is {x.bytes}/{MaxBytesPerLine} bytes"));
            return string.Join("\n", warnings);
        }
    }

    public bool HasWarning => Warning.Length > 0;
}