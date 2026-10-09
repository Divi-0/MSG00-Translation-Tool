using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Avalonia;
using MSG00.Translation.Application.Features.Csvb.Mapi.Conversation;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.UI.Models;

/// <summary>
/// One line of the conversation editor; <see cref="Depth"/> is the nesting inside yes/no choices.
/// </summary>
public abstract class ConversationRow : NotifyBase
{
    public int Depth { get; init; }

    public Thickness Indent => new(Depth * 32, 0, 0, 0);

    /// <summary>
    /// Turns the pointer groups of a conversation into editor rows.
    /// </summary>
    public static IEnumerable<ConversationRow> Create(ConversationCsvb conversationCsvb)
    {
        var depth = 0;
        foreach (var pointer in conversationCsvb.Pointers)
        {
            var group = pointer.Group.ToList();
            foreach (var entry in group)
            {
                switch (entry.Type)
                {
                    case ConversationPointerType.BranchEnd:
                        depth = depth > 0 ? depth - 1 : 0;
                        if (group.All(x => x.Type != ConversationPointerType.NoBranch))
                        {
                            yield return new ConversationMarkerRow { Depth = depth, Label = "End of choice" };
                        }
                        break;
                    case ConversationPointerType.YesBranch:
                        yield return new ConversationMarkerRow { Depth = depth, Label = "If YES" };
                        depth++;
                        break;
                    case ConversationPointerType.NoBranch:
                        yield return new ConversationMarkerRow { Depth = depth, Label = "If NO" };
                        depth++;
                        break;
                    case ConversationPointerType.Character when pointer.Slot.Length >= 8:
                        yield return new ConversationSpeakerRow
                        {
                            Depth = depth,
                            CharacterId = System.BitConverter.ToInt32(pointer.Slot, 0),
                            Variant = System.BitConverter.ToInt32(pointer.Slot, 4),
                        };
                        break;
                    case ConversationPointerType.Text or ConversationPointerType.TextWithParameters or ConversationPointerType.Question
                        when entry is ConversationPointerText text:
                        yield return new ConversationTextEntry(text) { Depth = depth };
                        break;
                }
            }
        }
    }
}

public sealed class ConversationMarkerRow : ConversationRow
{
    public required string Label { get; init; }
}

public sealed class ConversationSpeakerRow : ConversationRow
{
    /// <summary>ids confirmed by the introductions in C1_001 (1–5), C1_005 (6, 7) and C1_007 (8)</summary>
    private static readonly Dictionary<int, string> Names = new()
    {
        [1] = "Setsuna",
        [2] = "Lockon",
        [3] = "Allelujah",
        [4] = "Tieria",
        [5] = "Sumeragi",
        [6] = "Christina",
        [7] = "Feldt",
        [8] = "Wang Liu Mei",
    };

    public int CharacterId { get; init; }

    /// <summary>second slot value, probably the expression</summary>
    public int Variant { get; init; }

    public string Name => Names.TryGetValue(CharacterId, out var name) ? name : $"Character {CharacterId}";

    public string Label => $"{Name}  (id {CharacterId}, variant {Variant})";
}

public sealed class ConversationTextEntry : ConversationRow
{
    /// <summary>the text box fits 19 full-width characters per line (38 Shift-JIS bytes)</summary>
    public const int MaxBytesPerLine = 38;

    public const int MaxLines = 3;

    private static readonly Encoding ShiftJisEncoding = Encoding.GetEncoding("Shift-JIS");

    public ConversationTextEntry(ConversationPointerText pointer)
    {
        Pointer = pointer;
    }

    public ConversationPointerText Pointer { get; }

    public string Kind => Pointer.Type == ConversationPointerType.Question ? "Question (yes/no)" : "Text";

    public string Text
    {
        get => Pointer.TextLines;
        set
        {
            if (Pointer.TextLines == value)
            {
                return;
            }

            Pointer.TextLines = value;
            RaisePropertyChanged(nameof(Text));
            RaisePropertyChanged(nameof(Warning));
            RaisePropertyChanged(nameof(HasWarning));
            RaisePropertyChanged(nameof(HasTooManyLines));
        }
    }

    public bool HasTooManyLines => Text.Split('\n').Length > MaxLines;

    public bool CanRemove { get; set; }

    public ICommand? AddBoxCommand { get; set; }

    public ICommand? SplitCommand { get; set; }

    public ICommand? RemoveCommand { get; set; }

    public string Warning
    {
        get
        {
            var lines = Text.Split('\n');
            var warnings = lines
                .Select((line, index) => (line, index, bytes: ShiftJisEncoding.GetByteCount(line)))
                .Where(x => x.bytes > MaxBytesPerLine)
                .Select(x => $"Line {x.index + 1} is {x.bytes}/{MaxBytesPerLine} bytes and will not fit: \"{x.line}\"")
                .ToList();

            if (lines.Length > MaxLines)
            {
                warnings.Insert(0, $"{lines.Length} lines, the text box shows {MaxLines}");
            }

            return string.Join("\n", warnings);
        }
    }

    public bool HasWarning => Warning.Length > 0;
}