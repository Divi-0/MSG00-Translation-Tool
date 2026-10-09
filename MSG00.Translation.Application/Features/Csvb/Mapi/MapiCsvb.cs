using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.Mapi;

public class MapiCsvb : CsvbFile
{
    public static readonly byte[] MapiHeaderDefinitionBytes = "MAPI\0\0\0\0"u8.ToArray();
    
    public MapiCsvb() : base(CsvbFileType.MAPI)
    {
    }
        
    public required MapiHeader MapiHeader { get; set; }
}