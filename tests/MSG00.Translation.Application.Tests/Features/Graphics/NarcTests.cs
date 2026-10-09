using MSG00.Translation.Application.Features.Graphics;

namespace MSG00.Translation.Application.Tests.Features.Graphics;

public class NarcTests
{
    [Fact]
    public void Read_ReturnsFileNamesWithFolderAndDataOffsets()
    {
        var narc = NarcFileBuilder.Build();

        var entries = Narc.Read(narc);

        entries.Select(x => x.Name).ShouldBe(new[] { "dst/Other.NANR", "dst/Test_BG.NCGR", "dst/Test_BG.NCLR", "dst/Test_BG.NSCR" });
        entries.Select(x => System.Text.Encoding.ASCII.GetString(narc, x.Offset, 4)).ShouldBe(new[] { "RNAN", "RGCN", "RLCN", "RCSN" });
        entries.ShouldAllBe(x => BitConverter.ToInt32(narc, x.Offset + 8) == x.Length);
    }

    [Fact]
    public void Read_NoNarc_Throws()
    {
        Should.Throw<InvalidDataException>(() => Narc.Read(new byte[32]));
    }
}