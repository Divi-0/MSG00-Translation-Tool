using System.Linq;
using MSG00.Translation.Application.Features.Font;

namespace MSG00.Translation.UI.Models;

/// <summary>
/// Characters the game font can not show: Shift-JIS would write a look-alike (ä → a) or '?'. They can be added to
/// the font under "Font" in the ROM tree.
/// </summary>
public static class FontWarning
{
    /// <returns>the warning, empty when the font has every character</returns>
    public static string Of(string text) => GameEncoding.Instance.FindMissingCharacters(text) is { Count: > 0 } missing
        ? $"Not in the game font: {string.Join(" ", missing)} (add them under Font › Extra characters)"
        : string.Empty;
}