using MSG00.Translation.Domain.Files.Csvb;
using MSG00.Translation.Infrastructure.Domain.Enums;

namespace MSG00.Translation.Domain.Evm
{
    public class EvmCsvb : CsvbFile 
    {
        public EvmCsvb()
        {
            Type =  CsvbFileType.EVM_BASE;
        }

        public required int FileOffsetToTextTable { get; set; }
        public required int FileOffsetToOffsetTable { get; set; }
    }
}
