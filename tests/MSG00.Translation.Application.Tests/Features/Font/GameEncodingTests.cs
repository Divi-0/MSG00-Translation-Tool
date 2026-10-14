using System.Text;
using MSG00.Translation.Application.Features.Font;

namespace MSG00.Translation.Application.Tests.Features.Font;

public class GameEncodingTests
{
    private static readonly Encoding ShiftJis = GetShiftJis();

    private readonly GameEncoding _sut = new(new CharacterTable(new Dictionary<byte, char> { [0xA1] = 'ä', [0xA2] = 'ü', [0xA3] = 'ß' }));

    private static Encoding GetShiftJis()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding("Shift-JIS");
    }

    [Theory]
    [InlineData("ガンダム、エクシア！")]
    [InlineData("Exia, go! ･･･")]
    [InlineData("")]
    public void WithoutTable_IsShiftJis(string text)
    {
        var empty = new GameEncoding();

        empty.GetBytes(text).ShouldBe(ShiftJis.GetBytes(text));
        empty.GetString(ShiftJis.GetBytes(text)).ShouldBe(text);
    }

    [Fact]
    public void GetBytes_ExtraCharacters_AreOneByte()
    {
        _sut.GetBytes("Grüße").ShouldBe([(byte)'G', (byte)'r', 0xA2, 0xA3, (byte)'e']);
        _sut.GetByteCount("Grüße").ShouldBe(5);
    }

    [Fact]
    public void GetBytes_MixedWithJapanese()
    {
        var bytes = _sut.GetBytes("ä刹那ü");

        bytes.ShouldBe([0xA1, .. ShiftJis.GetBytes("刹那"), 0xA2]);
        _sut.GetString(bytes).ShouldBe("ä刹那ü");
    }

    [Fact]
    public void GetString_TrailByteOfTwoByteCharacter_IsNotAnExtraCharacter()
    {
        // ぃ = 82 A1: the trail byte is the slot of ä
        ShiftJis.GetBytes("ぃ").ShouldBe([0x82, 0xA1]);

        _sut.GetBytes("ぃä").ShouldBe([0x82, 0xA1, 0xA1]);
        _sut.GetString([0x82, 0xA1, 0xA1]).ShouldBe("ぃä");
        _sut.GetCharCount([0x82, 0xA1, 0xA1]).ShouldBe(2);
    }

    [Fact]
    public void WithoutTable_AccentIsLost()
    {
        // Shift-JIS writes a look-alike, which is why the editors warn about characters missing in the font
        new GameEncoding().GetBytes("ä").ShouldNotBe([0xA1]);
    }

    [Fact]
    public void GetString_UnusedSlot_StaysKatakana()
    {
        _sut.GetString([0xA5, 0xA4]).ShouldBe("･､");
    }

    [Fact]
    public void FindMissingCharacters_ListsWhatTheFontCanNotShow()
    {
        _sut.FindMissingCharacters("Grüße, Señor! ガンダム\n").ShouldBe(['ñ']);
        new GameEncoding().FindMissingCharacters("Grüße").ShouldBe(['ü', 'ß']);
    }

    [Fact]
    public void Characters_CanBeChanged()
    {
        var encoding = new GameEncoding();

        encoding.Characters = new CharacterTable(new Dictionary<byte, char> { [0xB0] = 'é' });

        encoding.GetBytes("é").ShouldBe([0xB0]);
    }
}