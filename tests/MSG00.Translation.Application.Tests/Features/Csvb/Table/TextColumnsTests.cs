using MSG00.Translation.Application.Features.Csvb.Table;

namespace MSG00.Translation.Application.Tests.Features.Csvb.Table;

public class TextColumnsTests
{
    [Theory]
    [InlineData("OG_MAP", 0, "Field name")]
    [InlineData("OG_MISSION", 6, "Mission name")]
    [InlineData("OG_MISSION", 7, "Mission description")]
    [InlineData("BLOCK1", 1, "Text")]
    [InlineData("BLOCK1", 0, "Requirement")]
    public void Find_TranslatableColumn(string table, int column, string label)
    {
        TextColumns.Find(table, column).ShouldNotBeNull().Label.ShouldBe(label);
    }

    [Theory]
    [InlineData("OG_CHAPTER", 10)] // conversation file names like "C1_001"
    [InlineData("OG_MISSION", 0)] // mission ids like "EVM_AEU001"
    [InlineData("OG_MAP", 4)]
    [InlineData("BLOCK1", 2)] // image names like "FG_01_01"
    public void Find_NotTranslatableColumn_ReturnsNull(string table, int column)
    {
        TextColumns.Find(table, column).ShouldBeNull();
    }
}