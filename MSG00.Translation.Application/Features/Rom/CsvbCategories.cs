using System.Text.RegularExpressions;

namespace MSG00.Translation.Application.Features.Rom;

/// <summary>
/// Groups the csvb files of the ROM by their path, in the order they are shown.
/// </summary>
public static class CsvbCategories
{
    public const string Other = "Other";

    private static readonly (string Name, Regex Pattern)[] Rules =
    {
        ("Missions", new Regex(@"^App/OG_.*\.csvb$", RegexOptions.IgnoreCase)),
        ("Prologue", new Regex(@"^2D/Prologue\.csvb$", RegexOptions.IgnoreCase)),
        ("Epilogue", new Regex(@"^2D/Epilogue\d+\.csvb$", RegexOptions.IgnoreCase)),
        ("Conversation", new Regex(@"^2D/Conv/C(\d+_\d+|_error)\.csvb$", RegexOptions.IgnoreCase)),
        ("Staff Roll", new Regex(@"^2D/StaffRoll\d*\.csvb$", RegexOptions.IgnoreCase)),
        ("Requirement", new Regex(@"^2D/requirement\.csvb$", RegexOptions.IgnoreCase)),
        ("ETC", new Regex(@"^2D/ETC_.*\.csvb$", RegexOptions.IgnoreCase)),
        ("FG", new Regex(@"^2D/FG_.*\.csvb$", RegexOptions.IgnoreCase)),
        ("HCM", new Regex(@"^2D/HCM_.*\.csvb$", RegexOptions.IgnoreCase)),
        ("HG", new Regex(@"^2D/HG_.*\.csvb$", RegexOptions.IgnoreCase)),
        ("Gunpla", new Regex(@"^2D/Gunpla_.*\.csvb$", RegexOptions.IgnoreCase)),
        ("Events", new Regex(@"^Ev/(AEU|ETC|HRL|UNI)/EVM_.*\.csvb$", RegexOptions.IgnoreCase)),
        ("Cut-ins", new Regex(@"^Cut/.*\.fcmd$", RegexOptions.IgnoreCase)),
        ("Events (test)", new Regex(@"^Ev/!test/.*\.csvb$", RegexOptions.IgnoreCase)),
        ("Graphics", new Regex(@"\.narc$", RegexOptions.IgnoreCase)),
    };

    /// <summary>
    /// Files without any text to translate (checked on the original ROM), left out of the ROM browser: map, unit and
    /// effect scripts (only internal names), definition and setting tables, the debug map list and the test mission
    /// scripts without messages.
    /// </summary>
    private static readonly Regex[] WithoutText =
    [
        new(@"^Map/.*\.fcmd$", RegexOptions.IgnoreCase),
        new(@"^Unit/FCmd/.*\.fcmd$", RegexOptions.IgnoreCase),
        new(@"^Eff/.*\.fcmd$", RegexOptions.IgnoreCase),
        new(@"^Dbg/", RegexOptions.IgnoreCase),
        new(@"^App/GameSetting\.csvb$", RegexOptions.IgnoreCase),
        new(@"^2D/LogueDefine\.csvb$", RegexOptions.IgnoreCase),
        new(@"^2D/Conv/(ConvDefine|Conversation)\.csvb$", RegexOptions.IgnoreCase),
        new(@"^Ev/!test/EVM_TEST_(?!XNKZ).*\.csvb$", RegexOptions.IgnoreCase),
    ];

    public static IReadOnlyList<string> Names { get; } = Rules.Select(x => x.Name).Append(Other).ToList();

    public static string Of(string path) => Rules.FirstOrDefault(x => x.Pattern.IsMatch(path)).Name ?? Other;

    public static bool HasText(string path) => !WithoutText.Any(x => x.IsMatch(path));
}