
namespace MSG00.Translation.Application.Features.Csvb
{
    public class CsvbFile : NotifyBase
    {
        protected CsvbFile(CsvbFileType type)
        {
            Type = type;
        }
        
        public CsvbFileType Type { get; }
        public required CsvbHeader CsvbHeader { get; init; }
        
        public static CsvbFile Create(CsvbFileType type, CsvbHeader header)
        {
            return new CsvbFile(type)
            {
                CsvbHeader = header
            };
        }
    }
}
