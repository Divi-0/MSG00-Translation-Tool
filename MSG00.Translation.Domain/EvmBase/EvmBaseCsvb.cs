using MSG00.Translation.Domain.Csvb;
using MSG00.Translation.Domain.Files.Csvb;

namespace MSG00.Translation.Domain.EvmBase
{
    public class EvmBaseCsvb : CsvbFile
    {
        public required EvmBaseHeader EvmBaseHeader { get; set; }
        public required byte[] EvSeqHeaderBytes { get; set; }
        public List<Pointer> PointerAndValue { get; set; } = new List<Pointer>();
    }
}
