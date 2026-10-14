namespace MSG00.Translation.Application.Features.Font;

public sealed record CharacterPreset(string Name, string Characters)
{
    public override string ToString() => $"{Name}: {Characters}";
}

/// <summary>
/// Sets of extra characters per language. Choosing a preset only adds the characters to the font editor; nothing
/// is written to the ROM until "Apply to ROM".
/// </summary>
public static class CharacterPresets
{
    public static CharacterPreset German { get; } = new("German", "ÄÖÜäöüß");

    public static CharacterPreset Spanish { get; } = new("Spanish", "ÁÉÍÓÚÑÜáéíóúñü¡¿");

    public static CharacterPreset French { get; } = new("French", "ÀÂÇÈÉÊËÎÏÔÙÛÜàâçèéêëîïôùûüŒœ«»");

    public static CharacterPreset Italian { get; } = new("Italian", "ÀÈÉÌÒÙàèéìòù");

    public static CharacterPreset Portuguese { get; } = new("Portuguese", "ÁÂÃÀÇÉÊÍÓÔÕÚáâãàçéêíóôõú");

    /// <summary>all of the above, 55 characters</summary>
    public static CharacterPreset WesternEuropean { get; } = new(
        "Western European (DE, ES, FR, IT, PT)",
        new string(new[] { German, Spanish, French, Italian, Portuguese }.SelectMany(x => x.Characters).Distinct().ToArray()));

    public static IReadOnlyList<CharacterPreset> All { get; } = [WesternEuropean, German, Spanish, French, Italian, Portuguese];
}