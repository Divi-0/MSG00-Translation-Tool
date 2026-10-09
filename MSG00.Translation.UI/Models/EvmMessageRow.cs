using System.Collections.Generic;
using System.Linq;
using System.Text;
using MSG00.Translation.Application.Features.Csvb.EvmBase;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.UI.Models;

/// <summary>
/// One message of a mission script (EV_SEQ command 0x67).
/// </summary>
public sealed class EvmMessageRow : NotifyBase
{
    private static readonly Encoding ShiftJisEncoding = Encoding.GetEncoding("Shift-JIS");

    public EvmMessageRow(EvmMessage message, string original)
    {
        Message = message;
        Original = original;
    }

    public EvmMessage Message { get; }

    public string Original { get; }

    public string Label => Message.Kind switch
    {
        EvmMessageKind.Box => $"#{Message.Index} · explanation box",
        EvmMessageKind.Battle => $"#{Message.Index} · battle message",
        _ => $"#{Message.Index} · message",
    };

    public string Text
    {
        get => Message.Pointer.Offset!.Text;
        set
        {
            if (Message.Pointer.Offset!.Text == value)
            {
                return;
            }

            Message.Pointer.Offset.Text = value;
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
            if (Message.MaxLines is { } maxLines && lines.Length > maxLines)
            {
                warnings.Add($"{lines.Length} lines, the box shows {maxLines}");
            }

            if (Message.MaxBytesPerLine is { } max)
            {
                warnings.AddRange(lines
                    .Select((line, index) => (index, bytes: ShiftJisEncoding.GetByteCount(line)))
                    .Where(x => x.bytes > max)
                    .Select(x => $"Line {x.index + 1} is {x.bytes}/{max} bytes and will be cut off"));
            }

            return string.Join("\n", warnings);
        }
    }

    public bool HasWarning => Warning.Length > 0;
}