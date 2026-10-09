namespace MSG00.Translation.Application.Features.Csvb
{
    public class CsvbHeader
    {
        public required int OffsetToEndOfPointerTable { get; set; }
        public required int OffsetToEndOfFullHeader { get; set; }
        public required byte[] OffsetToEndOfTextTable { get; set; } = Array.Empty<byte>();
        public required int OffsetToEndOfExtraTable { get; set; }
    }
}
