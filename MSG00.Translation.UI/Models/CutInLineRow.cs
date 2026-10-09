using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSG00.Translation.Application.Features.Csvb.CutIn;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.UI.Models;

/// <summary>
/// One cut-in line (command 0x34 of Cut/CUTIN.fcmd).
/// </summary>
public sealed class CutInLineRow : NotifyBase
{
    /// <summary>longest original lines have 20 full-width characters</summary>
    public const int MaxBytesPerLine = 40;

    public const int MaxLines = 2;

    private static readonly Encoding ShiftJisEncoding = Encoding.GetEncoding("Shift-JIS");

    public CutInLineRow(CutInLine line, int index)
    {
        Line = line;
        Index = index;
    }

    public CutInLine Line { get; }

    /// <summary>position in the file, key for import</summary>
    public int Index { get; }

    public string Label => $"{Line.Table} · shown {Line.DisplayTime / 4096} frames";

    public string Original => Line.Original;

    public string Text
    {
        get => Line.Text;
        set
        {
            if (Line.Text == value)
            {
                return;
            }

            Line.Text = value;
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
            var warnings = new List<string>();
            if (lines.Length > MaxLines)
            {
                warnings.Add($"{lines.Length} lines, cut-ins show {MaxLines}");
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