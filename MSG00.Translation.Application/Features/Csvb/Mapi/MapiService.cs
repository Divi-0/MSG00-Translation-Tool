using MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.Mapi;

public interface IMapiService
{
    Task<MapiCsvb> GetMapiCsvbAsync(CsvbFile csvbFile, Stream fileStream);
    Task SavePrologueAsync(Stream stream, PrologueCsvb prologueCsvb);
}

internal class MapiService : IMapiService
{
    private readonly IReader _reader;
    private readonly Conversation.IReader _conversationReader;
    private readonly StaffRoll.IReader _staffRollReader;

    public MapiService(IReader reader, Conversation.IReader conversationReader, StaffRoll.IReader staffRollReader)
    {
        _reader = reader;
        _conversationReader = conversationReader;
        _staffRollReader = staffRollReader;
    }

    public async Task<MapiCsvb> GetMapiCsvbAsync(CsvbFile csvbFile, Stream fileStream)
    {
        var mapiCsvb = new MapiCsvb
        {
            CsvbHeader = csvbFile.CsvbHeader,
            MapiHeader = new MapiHeader { FullHeaderBytes = await GetMapiHeader(fileStream).ConfigureAwait(false) },
        };
        
        return mapiCsvb.MapiHeader.MapiType switch
        {
            MapiType.Conversation or MapiType.ConversationWithoutType => await _conversationReader.ReadFileAsync(mapiCsvb, fileStream).ConfigureAwait(false),
            MapiType.PrologueEpilogue => await _reader.ReadFileAsync(mapiCsvb, fileStream).ConfigureAwait(false),
            MapiType.StaffRoll => await _staffRollReader.ReadFileAsync(mapiCsvb, fileStream).ConfigureAwait(false),
            _ => throw new NotImplementedException($"MapiType {(int)mapiCsvb.MapiHeader.MapiType} is not implemented")
        };
    }

    public async Task SavePrologueAsync(Stream stream, PrologueCsvb prologueCsvb)
    {
        //await _proEpilogueWriter.WriteFile(stream, prologueCsvb).ConfigureAwait(false);
    }
    
    protected static async Task<byte[]> GetMapiHeader(Stream fileStream)
    {
        fileStream.Seek(0x20, SeekOrigin.Begin);

        byte[] bytes = new byte[48];

        await fileStream.ReadExactlyAsync(bytes, 0, 48).ConfigureAwait(false);

        return bytes;
    }
}