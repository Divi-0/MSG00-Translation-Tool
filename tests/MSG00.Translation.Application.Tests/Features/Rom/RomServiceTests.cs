using Microsoft.Extensions.Logging.Abstractions;
using MSG00.Translation.Application.Features.Rom;

namespace MSG00.Translation.Application.Tests.Features.Rom;

public class RomServiceTests
{
    private readonly RomService _sut = new(NullLogger<RomService>.Instance);

    [Fact]
    public void NewService_HasNoRom()
    {
        _sut.IsOpen.ShouldBeFalse();
        _sut.HasChanges.ShouldBeFalse();
        _sut.CsvbFiles.ShouldBeEmpty();
    }

    [Fact]
    public void OpenFile_WithoutRom_Throws()
    {
        Should.Throw<InvalidOperationException>(() => _sut.OpenFile("2D/Prologue.csvb"));
    }

    [Fact]
    public void ReplaceFile_WithoutRom_Throws()
    {
        Should.Throw<InvalidOperationException>(() => _sut.ReplaceFile("2D/Prologue.csvb", [1, 2, 3]));
    }

    [Fact]
    public async Task SaveAs_WithoutRom_Throws()
    {
        using var output = new MemoryStream();

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.SaveAsAsync(output, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Open_InvalidData_Throws()
    {
        using var input = new MemoryStream(new byte[1024]);

        await Should.ThrowAsync<Exception>(() => _sut.OpenAsync(input, "invalid.nds", TestContext.Current.CancellationToken));
        _sut.IsOpen.ShouldBeFalse();
    }
}