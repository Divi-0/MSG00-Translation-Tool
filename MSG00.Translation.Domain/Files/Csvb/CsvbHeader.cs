namespace MSG00.Translation.Domain.Files.Csvb
{
    public class CsvbHeader
    {
        public required byte[] StaticFirstHeaderBytes { get; set; } = Array.Empty<byte>();
        public required int FileOffsetToAreaBetweenPointerAndTextTable { get; set; }
        public required int FileOffsetToTextTable { get; set; }
        public required byte[] FileOffsetToAreaBetweenTextAndOffsetTable { get; set; } = Array.Empty<byte>();
        public required int FileOffsetToOffsetTable { get; set; }

        public byte[] GetFullHeaderBytes()
        {
            return StaticFirstHeaderBytes
                .Concat(BitConverter.GetBytes(FileOffsetToAreaBetweenPointerAndTextTable))
                .Concat(BitConverter.GetBytes(FileOffsetToTextTable))
                .Concat(FileOffsetToAreaBetweenTextAndOffsetTable)
                .Concat(BitConverter.GetBytes(FileOffsetToOffsetTable))
                .Concat(new byte[4] {0, 0, 0, 0}) //static bytes at the end of the header
                .ToArray();
        }
    }
}
