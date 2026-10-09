using System.Text;
using MSG00.Translation.Application.Features.Arm9;

namespace MSG00.Translation.Application.Tests.Features.Arm9;

public class Arm9TextsTests
{
    private const int TextOffset = 0x10;
    private const int NextTextOffset = 0x20;
    private static readonly Arm9TextEntry Entry = new(TextOffset, Arm9Texts.InGameMenu, "Menu: mission select", "ミッション選択");
    private static readonly Arm9TextEntry FormatEntry = new(0xBAC4C, Arm9Texts.SaveLoad, "Slot info", "%02ddays       取得した戦術支援 %03d%");

    [Fact]
    public void Read_ReturnsTextUpToNul()
    {
        Arm9Texts.Read(CreateArm9(), Entry).ShouldBe("ミッション選択");
    }

    [Fact]
    public void MaxBytes_IsRoomUpToNextTextWithoutNul()
    {
        // 14 bytes text + NUL + 1 byte padding before the next text
        Arm9Texts.MaxBytes(CreateArm9(), Entry).ShouldBe(15);
    }

    [Fact]
    public void Write_ShorterText_FillsRestWithZeros()
    {
        var arm9 = CreateArm9();

        Arm9Texts.Write(arm9, Entry, "SAVE");

        Arm9Texts.Read(arm9, Entry).ShouldBe("SAVE");
        arm9[(TextOffset + 4)..NextTextOffset].ShouldAllBe(x => x == 0);
        arm9[NextTextOffset].ShouldBe((byte)'X');
        Arm9Texts.MaxBytes(arm9, Entry).ShouldBe(15);
    }

    [Fact]
    public void Write_TextUsingWholeRoom_KeepsTerminatingNul()
    {
        var arm9 = CreateArm9();

        Arm9Texts.Write(arm9, Entry, "Mission Select!");

        Arm9Texts.Read(arm9, Entry).ShouldBe("Mission Select!");
        arm9[NextTextOffset - 1].ShouldBe((byte)0);
        arm9[NextTextOffset].ShouldBe((byte)'X');
    }

    [Fact]
    public void Write_TooLongText_ThrowsAndKeepsOriginal()
    {
        var arm9 = CreateArm9();

        Should.Throw<ArgumentException>(() => Arm9Texts.Write(arm9, Entry, "Mission Select!!"));

        Arm9Texts.Read(arm9, Entry).ShouldBe("ミッション選択");
    }

    [Fact]
    public void Placeholders_FindsPrintfPlaceholders()
    {
        Arm9Texts.Placeholders("%02ddays       取得した戦術支援 %03d%").ShouldBe(new[] { "%02d", "%03d" });
        Arm9Texts.Placeholders("Load DATA-01?").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("%02d days     Tactical support %03d%", true)]
    [InlineData("%02d days", false)]
    [InlineData("%03d% support, %02d days", false)]
    public void HasSamePlaceholders_RequiresSamePlaceholdersInSameOrder(string text, bool expected)
    {
        Arm9Texts.HasSamePlaceholders(FormatEntry, text).ShouldBe(expected);
    }

    [Fact]
    public void Write_MissingPlaceholder_ThrowsAndKeepsOriginal()
    {
        var arm9 = CreateArm9();
        var entry = FormatEntry with { Offset = TextOffset };

        Should.Throw<ArgumentException>(() => Arm9Texts.Write(arm9, entry, "days"));

        Arm9Texts.Read(arm9, Entry).ShouldBe("ミッション選択");
    }

    [Fact]
    public void ByteCount_CountsShiftJisBytes()
    {
        Arm9Texts.ByteCount("SAVE").ShouldBe(4);
        Arm9Texts.ByteCount("ＳＡＶＥ").ShouldBe(8);
    }

    [Fact]
    public void Entries_HaveUniqueOffsets_AndBelongToKnownSections()
    {
        Arm9Texts.Entries.Select(x => x.Offset).ShouldBeUnique();
        Arm9Texts.Entries.ShouldAllBe(x => Arm9Texts.Sections.Contains(x.Section));
        Arm9Texts.Sections.ShouldContain(Arm9Texts.InGameMenu);
    }

    /// <summary>small ARM9 excerpt: non-zero code before, the text, NUL + padding, the next text</summary>
    private static byte[] CreateArm9()
    {
        var arm9 = Enumerable.Repeat((byte)0xFF, 0x30).ToArray();
        var text = Encoding.GetEncoding("Shift-JIS").GetBytes("ミッション選択");
        Array.Clear(arm9, TextOffset, NextTextOffset - TextOffset);
        text.CopyTo(arm9, TextOffset);
        arm9[NextTextOffset] = (byte)'X';
        return arm9;
    }
}