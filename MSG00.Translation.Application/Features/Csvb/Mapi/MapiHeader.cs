namespace MSG00.Translation.Application.Features.Csvb.Mapi;

public class MapiHeader
{
    public required byte[] FullHeaderBytes { get; init; }

    public MapiType MapiType => (MapiType)BitConverter.ToInt32(FullHeaderBytes.AsSpan()[44..48]);
}