namespace MSG00.Translation.Domain.Files.Csvb
{
    public class CsvbHeader
    {
        public required byte[] StaticFirstHeaderBytes { get; set; } = Array.Empty<byte>();
        public required byte[] FileOffsetToAreaBetweenPointerAndTextTable { get; set; } = Array.Empty<byte>();
        public required byte[] FileOffsetToTextTable { get; set; } = Array.Empty<byte>();
        public required byte[] FileOffsetToAreaBetweenTextAndOffsetTable { get; set; } = Array.Empty<byte>();
        public required byte[] FileOffsetToOffsetTable { get; set; } = Array.Empty<byte>();

        public byte[] GetFullHeaderBytes()
        {
            return StaticFirstHeaderBytes
                .Concat(FileOffsetToAreaBetweenPointerAndTextTable)
                .Concat(FileOffsetToTextTable)
                .Concat(FileOffsetToAreaBetweenTextAndOffsetTable)
                .Concat(FileOffsetToOffsetTable)
                .Concat(new byte[4] {0, 0, 0, 0}) //static bytes at the end of the header
                .ToArray();
        }
    }
}
