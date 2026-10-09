using Mediator;
using MSG00.Translation.Application.Features.Csvb.EvmBase.Read;
using MSG00.Translation.Application.Features.Csvb.Mapi;
using MSG00.Translation.Application.Features.Csvb.Read;
using MSG00.Translation.Application.Features.Csvb.EvmBase;
using MSG00.Translation.Application.Features.Csvb;
using MSG00.Translation.Application.Features.Csvb.Table;
using NSubstitute;
using TableReader = MSG00.Translation.Application.Features.Csvb.Table.IReader;
using CutInReader = MSG00.Translation.Application.Features.Csvb.CutIn.IReader;
using MSG00.Translation.Application.Features.Csvb.CutIn;

namespace MSG00.Translation.Application.Tests.Features.Csvb.Read;

public class ReadCsvbFileHandlerTests
{
    private static readonly CsvbHeader Header = new() { OffsetToEndOfPointerTable = 0, OffsetToEndOfFullHeader = 0, OffsetToEndOfTextTable = new byte[4], OffsetToEndOfExtraTable = 0 };

    private readonly IMediator _mediator = Substitute.For<IMediator>();
    private readonly ICsvbReader _csvbReader = Substitute.For<ICsvbReader>();
    private readonly IMapiService _mapiService = Substitute.For<IMapiService>();
    private readonly TableReader _tableReader = Substitute.For<TableReader>();
    private readonly CutInReader _cutInReader = Substitute.For<CutInReader>();
    private readonly ReadCsvbFileHandler _sut;
    private readonly Stream _stream = new MemoryStream();

    public ReadCsvbFileHandlerTests()
    {
        _sut = new ReadCsvbFileHandler(_mediator, _csvbReader, _mapiService, _tableReader, _cutInReader);
    }

    [Fact]
    public async Task Handle_MapiFile_ReturnsResultOfMapiService()
    {
        var csvbFile = CsvbFile.Create(CsvbFileType.MAPI, Header);
        var mapiCsvb = new MapiCsvb { CsvbHeader = Header, MapiHeader = new MapiHeader { FullHeaderBytes = new byte[48] } };
        _csvbReader.ReadAsync(_stream, Arg.Any<CancellationToken>()).Returns(csvbFile);
        _mapiService.GetMapiCsvbAsync(csvbFile, _stream).Returns(mapiCsvb);

        var result = await _sut.Handle(new ReadCsvbFile(_stream), TestContext.Current.CancellationToken);

        result.ShouldBeSameAs(mapiCsvb);
    }

    [Fact]
    public async Task Handle_EvmBaseFile_SendsReadEvmBase()
    {
        var csvbFile = CsvbFile.Create(CsvbFileType.EVM_BASE, Header);
        var evm = new EvmBaseCsvb { CsvbHeader = Header, EvmBaseHeader = new EvmBaseHeader { HeaderInformation = [], FileOffsetToPointerTable = 0 }, EvSeqHeaderBytes = [] };
        _csvbReader.ReadAsync(_stream, Arg.Any<CancellationToken>()).Returns(csvbFile);
        _mediator.Send(Arg.Is<ReadEvmBase>(x => x.CsvbFile == csvbFile && x.Stream == _stream), Arg.Any<CancellationToken>()).Returns(evm);

        var result = await _sut.Handle(new ReadCsvbFile(_stream), TestContext.Current.CancellationToken);

        result.ShouldBeSameAs(evm);
        await _mapiService.DidNotReceiveWithAnyArgs().GetMapiCsvbAsync(default!, default!);
    }

    [Fact]
    public async Task Handle_BlockFile_UsesTableReader()
    {
        var csvbFile = CsvbFile.Create(CsvbFileType.BLOCK, Header);
        var table = new TableCsvb(CsvbFileType.BLOCK) { CsvbHeader = Header, Bytes = [], Tables = [], Texts = [] };
        _csvbReader.ReadAsync(_stream, Arg.Any<CancellationToken>()).Returns(csvbFile);
        _tableReader.ReadFileAsync(csvbFile, _stream).Returns(table);

        var result = await _sut.Handle(new ReadCsvbFile(_stream), TestContext.Current.CancellationToken);

        result.ShouldBeSameAs(table);
        await _mapiService.DidNotReceiveWithAnyArgs().GetMapiCsvbAsync(default!, default!);
    }

    [Fact]
    public async Task Handle_TableFile_UsesTableReader()
    {
        var csvbFile = CsvbFile.Create(CsvbFileType.OG_TABLE, Header);
        var table = new TableCsvb { CsvbHeader = Header, Bytes = [], Tables = [], Texts = [] };
        _csvbReader.ReadAsync(_stream, Arg.Any<CancellationToken>()).Returns(csvbFile);
        _tableReader.ReadFileAsync(csvbFile, _stream).Returns(table);

        var result = await _sut.Handle(new ReadCsvbFile(_stream), TestContext.Current.CancellationToken);

        result.ShouldBeSameAs(table);
        await _mapiService.DidNotReceiveWithAnyArgs().GetMapiCsvbAsync(default!, default!);
    }

    [Fact]
    public async Task Handle_CutInFile_UsesCutInReader()
    {
        var csvbFile = CsvbFile.Create(CsvbFileType.CUT_IN, Header);
        var cutIn = new CutInCsvb { CsvbHeader = Header, Bytes = [], Lines = [] };
        _csvbReader.ReadAsync(_stream, Arg.Any<CancellationToken>()).Returns(csvbFile);
        _cutInReader.ReadFileAsync(csvbFile, _stream).Returns(cutIn);

        var result = await _sut.Handle(new ReadCsvbFile(_stream), TestContext.Current.CancellationToken);

        result.ShouldBeSameAs(cutIn);
    }
}
