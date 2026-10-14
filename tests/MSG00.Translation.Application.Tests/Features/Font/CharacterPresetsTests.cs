using MSG00.Translation.Application.Features.Font;

namespace MSG00.Translation.Application.Tests.Features.Font;

public class CharacterPresetsTests
{
    [Fact]
    public void WesternEuropean_HasAllLanguages_InFreeSlots()
    {
        var characters = CharacterPresets.WesternEuropean.Characters;

        characters.Length.ShouldBe(55);
        characters.Distinct().Count().ShouldBe(55);
        characters.Length.ShouldBeLessThanOrEqualTo(CharacterTable.Slots.Count);
        foreach (var preset in CharacterPresets.All)
        {
            preset.Characters.ShouldAllBe(c => characters.Contains(c));
        }
    }

    [Fact]
    public void EveryPresetCharacter_CanBeAdded()
    {
        foreach (var character in CharacterPresets.WesternEuropean.Characters)
        {
            CharacterTable.Check(character).ShouldBeNull(character.ToString());
        }
    }
}