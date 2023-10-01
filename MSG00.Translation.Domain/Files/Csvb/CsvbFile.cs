using MSG00.Translation.Infrastructure.Domain.Enums;
using MSG00.Translation.Infrastructure.Domain.Misc;

namespace MSG00.Translation.Domain.Files.Csvb
{
    public class CsvbFile : NotifyBase
    {
        public CsvbFileType Type { get; init; }
        public required CsvbHeader Header { get; init; }
    }
}
