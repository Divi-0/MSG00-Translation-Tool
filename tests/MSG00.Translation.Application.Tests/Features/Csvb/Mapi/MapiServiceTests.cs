using MSG00.Translation.Application.Features.Csvb.Mapi;
using MSG00.Translation.Application.Features.Csvb.Mapi.Conversation;
using MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue;
using MSG00.Translation.Application.Features.Csvb.Read;
using NSubstitute;
using ConversationReader = MSG00.Translation.Application.Features.Csvb.Mapi.Conversation.IReader;
using PrologueReader = MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue.IReader;
using StaffRollReader = MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll.IReader;
using MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll;

namespace MSG00.Translation.Application.Tests.Features.Csvb.Mapi;

public class MapiServiceTests
{
    private readonly PrologueReader _prologueReader = Substitute.For<PrologueReader>();
    private readonly ConversationReader _conversationReader = Substitute.For<ConversationReader>();
    private readonly StaffRollReader _staffRollReader = Substitute.For<StaffRollReader>();
    private readonly MapiService _sut;

    public MapiServiceTests()
    {
        _sut = new MapiService(_prologueReader, _conversationReader, _staffRollReader);
    }

    [Fact]
    public async Task GetMapiCsvb_PrologueEpilogueType_UsesPrologueReader()
    {
        var expected = new PrologueCsvb { CsvbHeader = null!, MapiHeader = null!, AfterTextSectionBytes = [] };
        _prologueReader.ReadFileAsync(Arg.Any<MapiCsvb>(), Arg.Any<Stream>()).Returns(expected);

        var result = await ReadAsync(MapiFileBuilder.PrologueEpilogueType);

        result.ShouldBeSameAs(expected);
        await _conversationReader.DidNotReceiveWithAnyArgs().ReadFileAsync(default!, default!);
    }

    [Fact]
    public async Task GetMapiCsvb_ConversationType_UsesConversationReaderWithMapiHeader()
    {
        var expected = new ConversationCsvb { CsvbHeader = null!, MapiHeader = null!, AfterTextSectionBytes = [], OffsetTablePrefix = [] };
        _conversationReader.ReadFileAsync(Arg.Any<MapiCsvb>(), Arg.Any<Stream>()).Returns(expected);

        var result = await ReadAsync(MapiFileBuilder.ConversationType);

        result.ShouldBeSameAs(expected);
        await _conversationReader.Received(1).ReadFileAsync(
            Arg.Is<MapiCsvb>(x => x.MapiHeader.MapiType == MapiType.Conversation && x.MapiHeader.FullHeaderBytes.Length == 48),
            Arg.Any<Stream>());
        await _prologueReader.DidNotReceiveWithAnyArgs().ReadFileAsync(default!, default!);
    }

    [Fact]
    public async Task GetMapiCsvb_StaffRollType_UsesStaffRollReader()
    {
        var expected = new StaffRollTextCsvb { CsvbHeader = null!, MapiHeader = null!, Bytes = [], Texts = [] };
        _staffRollReader.ReadFileAsync(Arg.Any<MapiCsvb>(), Arg.Any<Stream>()).Returns(expected);

        var result = await ReadAsync(MapiFileBuilder.StaffRollType);

        result.ShouldBeSameAs(expected);
        await _conversationReader.DidNotReceiveWithAnyArgs().ReadFileAsync(default!, default!);
    }

    [Fact]
    public async Task GetMapiCsvb_TypeZero_UsesConversationReader()
    {
        var expected = new ConversationCsvb { CsvbHeader = null!, MapiHeader = null!, AfterTextSectionBytes = [], OffsetTablePrefix = [] };
        _conversationReader.ReadFileAsync(Arg.Any<MapiCsvb>(), Arg.Any<Stream>()).Returns(expected);

        var result = await ReadAsync(0x00);

        result.ShouldBeSameAs(expected);
    }

    [Fact]
    public async Task GetMapiCsvb_NotImplementedType_Throws()
    {
        await Should.ThrowAsync<NotImplementedException>(() => ReadAsync(0x01));
    }

    private async Task<MapiCsvb> ReadAsync(int mapiType)
    {
        var bytes = new MapiFileBuilder(mapiType).Character(1, 1).Text("text").Build();
        using var stream = new MemoryStream(bytes, writable: false);
        var csvbFile = await new CsvbReader().ReadAsync(stream, TestContext.Current.CancellationToken);
        return await _sut.GetMapiCsvbAsync(csvbFile, stream);
    }
}