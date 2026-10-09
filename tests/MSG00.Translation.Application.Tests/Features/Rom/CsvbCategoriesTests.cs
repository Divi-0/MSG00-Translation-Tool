using MSG00.Translation.Application.Features.Rom;

namespace MSG00.Translation.Application.Tests.Features.Rom;

public class CsvbCategoriesTests
{
    [Theory]
    [InlineData("App/OG_MapMission.csvb", "Missions")]
    [InlineData("2D/Prologue.csvb", "Prologue")]
    [InlineData("2D/Epilogue1.csvb", "Epilogue")]
    [InlineData("2D/Epilogue7.csvb", "Epilogue")]
    [InlineData("2D/Conv/C1_001.csvb", "Conversation")]
    [InlineData("2D/Conv/C4_007.csvb", "Conversation")]
    [InlineData("2D/StaffRoll.csvb", "Staff Roll")]
    [InlineData("2D/StaffRoll2.csvb", "Staff Roll")]
    [InlineData("2D/requirement.csvb", "Requirement")]
    [InlineData("2D/ETC___EXIA.csvb", "ETC")]
    [InlineData("2D/FG_EXIA.csvb", "FG")]
    [InlineData("2D/HCM_Pro_EXIA.csvb", "HCM")]
    [InlineData("2D/HG_TIEREN.csvb", "HG")]
    [InlineData("2D/Gunpla_Lv1.csvb", "Gunpla")]
    [InlineData("Ev/AEU/EVM_AEU01.csvb", "Events")]
    [InlineData("Ev/!test/EVM_TEST_DEMO.csvb", "Events (test)")]
    [InlineData("Cut/CUTIN.fcmd", "Cut-ins")]
    [InlineData("2D/Conv/C_error.csvb", "Conversation")]
    [InlineData("2D/Conv/ConvDefine.csvb", CsvbCategories.Other)]
    [InlineData("2D/LogueDefine.csvb", CsvbCategories.Other)]
    [InlineData("App/GameSetting.csvb", CsvbCategories.Other)]
    public void Of_ReturnsCategoryByPath(string path, string expected)
    {
        CsvbCategories.Of(path).ShouldBe(expected);
    }

    [Theory]
    [InlineData("Map/MA_AA00_000.fcmd")]
    [InlineData("Unit/FCmd/GEX0.fcmd")]
    [InlineData("Eff/EFFECT.fcmd")]
    [InlineData("Dbg/MAP_INFO.csvb")]
    [InlineData("App/GameSetting.csvb")]
    [InlineData("2D/LogueDefine.csvb")]
    [InlineData("2D/Conv/ConvDefine.csvb")]
    [InlineData("2D/Conv/Conversation.csvb")]
    [InlineData("Ev/!test/EVM_TEST_DEMO.csvb")]
    public void HasText_FileWithoutText_IsFalse(string path)
    {
        CsvbCategories.HasText(path).ShouldBeFalse();
    }

    [Theory]
    [InlineData("2D/Conv/C_error.csvb")]
    [InlineData("2D/Conv/C1_001.csvb")]
    [InlineData("Ev/!test/EVM_TEST_XNKZ.csvb")]
    [InlineData("Cut/CUTIN.fcmd")]
    [InlineData("2D/Briefing.narc")]
    public void HasText_FileWithText_IsTrue(string path)
    {
        CsvbCategories.HasText(path).ShouldBeTrue();
    }

    [Fact]
    public void Names_ContainEveryCategoryOnce_WithOtherLast()
    {
        CsvbCategories.Names.ShouldBeUnique();
        CsvbCategories.Names[^1].ShouldBe(CsvbCategories.Other);
    }
}