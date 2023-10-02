using MSG00.Translation.Infrastructure.Domain.Etc;

namespace MSG00.Translation.Infrastructure.Reader.EtcFgHcmHg
{
    internal sealed class EtcFgHcmHgReader : BlockReader, IEtcFgHcmHgReader
    {
        public async Task<EtcFgHcmHgCsvb> ReadFile(Stream stream)
        {
            throw new NotImplementedException();

            //int fileSizePointerTable = await GetFileSizePointerTable(stream).ConfigureAwait(false);
            //int fileSizeFullHeader = await GetFileSizeFullHeader(stream).ConfigureAwait(false);

            //List<EtcFgHcmHgPointer> etcPointers = new List<EtcFgHcmHgPointer>();

            //stream.Seek(0x4C, SeekOrigin.Begin);

            //while (stream.Position < fileSizePointerTable)
            //{
            //    long currentPositionInPointerTable = stream.Position;

            //    byte[] textOffsetBytes = new byte[4];
            //    await stream.ReadExactlyAsync(textOffsetBytes, 0, 4).ConfigureAwait(false);
            //    long textOffsetInTextTable = BitConverter.ToInt32(textOffsetBytes);

            //    byte[] gameObjectOffsetBytes = new byte[4];
            //    await stream.ReadExactlyAsync(gameObjectOffsetBytes, 0, 4).ConfigureAwait(false);
            //    long gameObjectOffsetInTextTable = BitConverter.ToInt32(gameObjectOffsetBytes);

            //    etcPointers.Add(new EtcFgHcmHgPointer
            //    {
            //        Text = await GetTextInFile(stream, Convert.ToInt64(fileSizeFullHeader) + textOffsetInTextTable).ConfigureAwait(false),
            //        GameObjectReference = await GetTextInFile(stream, Convert.ToInt64(fileSizeFullHeader) + gameObjectOffsetInTextTable).ConfigureAwait(false)
            //    });

            //    stream.Seek(currentPositionInPointerTable + 0x14, SeekOrigin.Begin);
            //}

            //return new EtcFgHcmHgCsvb
            //{
            //    FullHeaderSize = 0,
            //    HeaderBytes = Array.Empty<byte>(),
            //    FileOffsetToAreaBetweenPointerAndTextTable = 0,
            //    EtcPointers = etcPointers
            //};
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