using MSG00.Translation.Domain.Files.Csvb;
using MSG00.Translation.Infrastructure.Domain.Enums;

namespace MSG00.Translation.Infrastructure.Domain.Etc
{
    public sealed class EtcFgHcmHgCsvb : CsvbFile
    {
        public EtcFgHcmHgCsvb()
        {
            Type = CsvbFileType.BLOCK;
        }

        public IReadOnlyList<EtcFgHcmHgPointer> EtcPointers { get; init; } = new List<EtcFgHcmHgPointer>();
    }
}
