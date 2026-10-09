using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSG00.Translation.Application.Features.Csvb.Table;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.UI.Models;

/// <summary>
/// One translatable cell of a csvb table (e.g. a field name or a mission description).
/// </summary>
public sealed class TableTextRow : NotifyBase
{
    private static readonly Encoding ShiftJisEncoding = Encoding.GetEncoding("Shift-JIS");

    public TableTextRow(TableText text, TextColumn column, string rowLabel)
    {
        TableText = text;
        Column = column;
        RowLabel = rowLabel;
    }

    public TableText TableText { get; }

    public TextColumn Column { get; }

    public string RowLabel { get; }

    public string Original => TableText.Original;

    public string Text
    {
        get => TableText.Text;
        set
        {
            if (TableText.Text == value)
            {
                return;
            }

            TableText.Text = value;
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
            if (Column.MaxLines is { } maxLines && lines.Length > maxLines)
            {
                warnings.Add($"{lines.Length} lines, the game shows {maxLines}");
            }

            if (Column.MaxBytesPerLine is { } max)
            {
                warnings.AddRange(lines
                    .Select((line, index) => (line, index, bytes: ShiftJisEncoding.GetByteCount(line)))
                    .Where(x => x.bytes > max)
                    .Select(x => $"Line {x.index + 1} is {x.bytes}/{max} bytes and will be cut off"));
            }

            return string.Join("\n", warnings);
        }
    }

    public bool HasWarning => Warning.Length > 0;
}