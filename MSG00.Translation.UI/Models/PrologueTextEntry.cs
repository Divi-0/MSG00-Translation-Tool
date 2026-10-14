using MSG00.Translation.Application.Features.Font;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MSG00.Translation.Application.Features.Csvb;
using MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue;

namespace MSG00.Translation.UI.Models;

/// <summary>
/// One prologue text and the part of the script it stays on screen for: every Image after it until the next
/// pointer group containing <see cref="ProloguePointerType.ClearText"/>, which clears the text.
/// An Image value is the number of frames that image (and its animation) is shown.
/// </summary>
public class PrologueTextEntry : NotifyBase
{
    /// <summary>bottom screen is 256 px wide, the font uses 6 px per Shift-JIS byte (12 px for full-width)</summary>
    public const int MaxBytesPerLine = 42;

    /// <summary>typewriter speed of the prologue text, measured in the emulator (~4.1)</summary>
    public const int FramesPerCharacter = 4;

    /// <summary>time the fully typed text should stay readable (~3.5 s after typing measured in the emulator)</summary>
    public const int ReadingFrames = 240;

    public const double FramesPerSecond = 60;

    private static readonly Encoding ShiftJisEncoding = GameEncoding.Instance;

    private readonly IReadOnlyList<ProloguePointer> _images;
    private readonly int _transitionFrames;

    private PrologueTextEntry(ProloguePointerText pointer, IReadOnlyList<ProloguePointer> images, int transitionFrames)
    {
        Pointer = pointer;
        _images = images;
        _transitionFrames = transitionFrames;
    }

    public static IEnumerable<PrologueTextEntry> CreateEntries(IReadOnlyList<ProloguePointer> pointers)
    {
        for (var i = 0; i < pointers.Count; i++)
        {
            if (pointers[i] is not ProloguePointerText text)
            {
                continue;
            }

            var images = new List<ProloguePointer>();
            var transitionFrames = 0;
            var pendingTransitionFrames = 0;
            for (var j = i + 1; j < pointers.Count && !ClearsText(pointers[j]) && pointers[j] is not ProloguePointerText; j++)
            {
                switch (pointers[j].Type)
                {
                    case ProloguePointerType.ShowImage:
                        // only fades between two images count, the fade after the last image already hides the text
                        transitionFrames += images.Count > 0 ? pendingTransitionFrames : 0;
                        pendingTransitionFrames = 0;
                        images.Add(pointers[j]);
                        break;
                    // fades: upper 4 bytes hold the length in frames
                    case ProloguePointerType.TransitionBefore:
                    case ProloguePointerType.TransitionAfter:
                        pendingTransitionFrames += (int)(pointers[j].OffsetValue >> 32);
                        break;
                }
            }

            yield return new PrologueTextEntry(text, images, transitionFrames);
        }

        static bool ClearsText(ProloguePointer pointer) =>
            pointer.Type == ProloguePointerType.ClearText || pointer.SubPointer.Any(x => x.Type == ProloguePointerType.ClearText);
    }

    public ProloguePointerText Pointer { get; }

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
            RaisePropertyChanged(nameof(LineWarning));
            RaisePropertyChanged(nameof(HasLineWarning));
            RaiseDurationChanged();
        }
    }

    public bool HasDuration => _images.Count > 0;

    public int ImageCount => _images.Count;

    /// <summary>frames of the last image before the text is cleared; extending it keeps the pace of earlier images (montages)</summary>
    public int Duration
    {
        get => HasDuration ? (int)_images[^1].OffsetValue : 0;
        set
        {
            if (!HasDuration || _images[^1].OffsetValue == value)
            {
                return;
            }

            _images[^1].OffsetValue = value;
            RaiseDurationChanged();
        }
    }

    /// <summary>frames the text stays on screen: all images plus the fades between them</summary>
    public int OnScreenFrames => (int)_images.Sum(x => x.OffsetValue) + _transitionFrames;

    public string OnScreenText => ImageCount > 1
        ? $"On screen {OnScreenFrames / FramesPerSecond:0.0} s ({ImageCount} images)"
        : $"On screen {OnScreenFrames / FramesPerSecond:0.0} s";

    /// <summary>frames needed to type the whole text and leave it readable for <see cref="ReadingFrames"/></summary>
    public int SuggestedOnScreenFrames => FramesPerCharacter * Text.Replace("\n", string.Empty).Length + ReadingFrames;

    public bool IsDurationTooShort => HasDuration && OnScreenFrames < SuggestedOnScreenFrames;

    public string DurationHint => IsDurationTooShort
        ? $"Too short: typing + reading needs ~{SuggestedOnScreenFrames / FramesPerSecond:0.0} s, last image needs +{SuggestedOnScreenFrames - OnScreenFrames} frames"
        : string.Empty;

    public string LineWarning => string.Join("\n", Text.Split('\n')
        .Select((line, index) => (line, index, bytes: ShiftJisEncoding.GetByteCount(line)))
        .Where(x => x.bytes > MaxBytesPerLine)
        .Select(x => $"Line {x.index + 1} is {x.bytes}/{MaxBytesPerLine} bytes and will be cut off: \"{x.line}\"")
        .Append(FontWarning.Of(Text))
        .Where(x => x.Length > 0));

    public bool HasLineWarning => LineWarning.Length > 0;

    public void ApplySuggestedDuration()
    {
        if (IsDurationTooShort)
        {
            Duration += SuggestedOnScreenFrames - OnScreenFrames;
        }
    }

    public ICommand ApplySuggestedDurationCommand => new RelayCommand(ApplySuggestedDuration);

    private void RaiseDurationChanged()
    {
        RaisePropertyChanged(nameof(Duration));
        RaisePropertyChanged(nameof(OnScreenFrames));
        RaisePropertyChanged(nameof(OnScreenText));
        RaisePropertyChanged(nameof(SuggestedOnScreenFrames));
        RaisePropertyChanged(nameof(IsDurationTooShort));
        RaisePropertyChanged(nameof(DurationHint));
    }
}