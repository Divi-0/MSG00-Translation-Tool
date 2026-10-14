using MSG00.Translation.Application.Features.Arm9;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.UI.Models;

/// <summary>
/// One text of the ARM9 program; it can only be replaced in place, so it must not exceed <see cref="MaxBytes"/>.
/// </summary>
public sealed class ProgramTextRow : NotifyBase
{
    private string _text;

    public ProgramTextRow(Arm9TextEntry entry, string text, int maxBytes)
    {
        Entry = entry;
        _text = text;
        MaxBytes = maxBytes;
    }

    public Arm9TextEntry Entry { get; }

    public string Label => Entry.Label;

    public string Japanese => Entry.Japanese;

    public int MaxBytes { get; }

    public string Text
    {
        get => _text;
        set
        {
            if (SetProperty(ref _text, value))
            {
                RaisePropertyChanged(nameof(Usage));
                RaisePropertyChanged(nameof(IsTooLong));
                RaisePropertyChanged(nameof(PlaceholderWarning));
                RaisePropertyChanged(nameof(HasPlaceholderWarning));
                RaisePropertyChanged(nameof(MissingCharactersWarning));
                RaisePropertyChanged(nameof(HasMissingCharactersWarning));
            }
        }
    }

    public string MissingCharactersWarning => FontWarning.Of(Text);

    public bool HasMissingCharactersWarning => MissingCharactersWarning.Length > 0;

    /// <summary>texts like "%02ddays" are filled in by the game; the placeholders must stay</summary>
    public string PlaceholderWarning => Arm9Texts.HasSamePlaceholders(Entry, Text)
        ? string.Empty
        : $"keep {string.Join(" ", Arm9Texts.Placeholders(Entry.Japanese))} in this order";

    public bool HasPlaceholderWarning => PlaceholderWarning.Length > 0;

    public string Usage => $"{Arm9Texts.ByteCount(Text)}/{MaxBytes} bytes";

    public bool IsTooLong => Arm9Texts.ByteCount(Text) > MaxBytes;
}