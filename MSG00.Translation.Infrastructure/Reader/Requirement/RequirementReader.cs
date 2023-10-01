using MSG00.Translation.Infrastructure.Domain.Requirement;

namespace MSG00.Translation.Infrastructure.Reader.Requirement
{
    internal sealed class RequirementReader : BlockReader, IRequirementReader
    {
        public async Task<RequirementCsvb> ReadFile(Stream stream)
        {
            int fileSizePointerTable = await GetFileSizePointerTable(stream).ConfigureAwait(false);
            int fileSizeFullHeader = await GetFileSizeFullHeader(stream).ConfigureAwait(false);

            List<RequirementPointer> etcPointers = new List<RequirementPointer>();

            stream.Seek(0x48, SeekOrigin.Begin);

            while (stream.Position < fileSizePointerTable)
            {
                byte[] textOffsetBytes = new byte[4];
                await stream.ReadExactlyAsync(textOffsetBytes, 0, 4).ConfigureAwait(false);
                long textOffsetInTextTable = BitConverter.ToInt32(textOffsetBytes);

                etcPointers.Add(new RequirementPointer
                {
                    Text = await GetTextInFile(stream, Convert.ToInt64(fileSizeFullHeader) + textOffsetInTextTable).ConfigureAwait(false)
                });
            }

            return new RequirementCsvb
            {
                FullHeaderSize = 0,
                HeaderBytes = Array.Empty<byte>(),
                FileOffsetToAreaBetweenPointerAndTextTable = 0,
                Pointers = etcPointers
            };
        }

        private async Task<int> GetFileSizePointerTable(Stream stream)
        {
            byte[] bytes = new byte[4];

            stream.Seek(0x0C, SeekOrigin.Begin);
            await stream.ReadExactlyAsync(bytes, 0, 4).ConfigureAwait(false);

            return BitConverter.ToInt32(bytes);
        }

        private async Task<int> GetFileSizeFullHeader(Stream stream)
        {
            byte[] bytes = new byte[4];

            stream.Seek(0x10, SeekOrigin.Begin);
            await stream.ReadExactlyAsync(bytes, 0, 4).ConfigureAwait(false);

            return BitConverter.ToInt32(bytes);
        }
    }
}