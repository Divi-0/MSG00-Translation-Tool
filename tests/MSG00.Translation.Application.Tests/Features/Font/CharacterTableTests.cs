using MSG00.Translation.Application.Features.Font;

namespace MSG00.Translation.Application.Tests.Features.Font;

public class CharacterTableTests
{
    [Fact]
    public void Slots_AreHalfWidthKatakana_WithoutTheMiddleDot()
    {
        CharacterTable.Slots.Count.ShouldBe(62);
        CharacterTable.Slots[0].ShouldBe((byte)0xA1);
        CharacterTable.Slots[^1].ShouldBe((byte)0xDF);
        CharacterTable.Slots.ShouldNotContain((byte)0xA5);
    }

    [Theory]
    [InlineData('ä')]
    [InlineData('ß')]
    [InlineData('¿')]
    [InlineData('Œ')]
    [InlineData('€')]
    public void Check_NewCharacter_IsAccepted(char character)
    {
        CharacterTable.Check(character).ShouldBeNull();
    }

    [Theory]
    [InlineData('a')]
    [InlineData('あ')]
    [InlineData('°')]
    [InlineData('ｱ')]
    [InlineData(' ')]
    [InlineData('\n')]
    public void Check_CharacterOfTheFont_IsRefused(char character)
    {
        CharacterTable.Check(character).ShouldNotBeNull();
    }

    [Fact]
    public void Bytes_AreReadBack()
    {
        var table = new CharacterTable(new Dictionary<byte, char> { [0xA1] = 'ä', [0xDF] = 'Œ' });

        var bytes = table.ToBytes();
        var reread = CharacterTable.FromBytes(count => bytes.Take(count).ToArray());

        bytes.Length.ShouldBe(CharacterTable.SizeOf(2));
        reread.Characters.ShouldBe(table.Characters);
        reread.TryGetSlot('Œ', out var slot).ShouldBeTrue();
        slot.ShouldBe((byte)0xDF);
    }

    [Fact]
    public void FromBytes_WithoutTable_IsEmpty()
    {
        CharacterTable.FromBytes(count => new byte[count]).IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void FromBytes_BrokenTable_IsEmpty()
    {
        // slot 0x41 is not a free slot
        byte[] bytes = [.. "MSGC"u8, 1, 1, 0x41, 0xE4, 0x00];

        CharacterTable.FromBytes(count => bytes.Take(count).ToArray()).IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void New_ReservedSlot_Throws()
    {
        Should.Throw<ArgumentException>(() => new CharacterTable(new Dictionary<byte, char> { [0xA5] = 'ä' }));
    }

    [Fact]
    public void New_SameCharacterTwice_Throws()
    {
        Should.Throw<ArgumentException>(() => new CharacterTable(new Dictionary<byte, char> { [0xA1] = 'ä', [0xA2] = 'ä' }));
    }

    [Fact]
    public void New_CharacterOfTheFont_Throws()
    {
        Should.Throw<ArgumentException>(() => new CharacterTable(new Dictionary<byte, char> { [0xA1] = 'a' }));
    }
}