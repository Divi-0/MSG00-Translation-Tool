using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.EvmBase
{
    public class EvmBaseCsvb : CsvbFile
    {
        public EvmBaseCsvb() : base(CsvbFileType.EVM_BASE)
        {
        }

        public required EvmBaseHeader EvmBaseHeader { get; set; }
        public required byte[] EvSeqHeaderBytes { get; set; }
        public List<Pointer> PointerAndValue { get; set; } = new List<Pointer>();

        /// <summary>
        /// the original file; the writer keeps it and only changes message texts (see docs/evm/README.md)
        /// </summary>
        public byte[] Bytes { get; init; } = Array.Empty<byte>();
    }
}
