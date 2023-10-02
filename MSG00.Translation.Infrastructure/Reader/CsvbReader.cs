using MSG00.Translation.Domain.EvmBase.Enums;
using MSG00.Translation.Domain.Files.Csvb;
using MSG00.Translation.Infrastructure.Domain.Enums;
using MSG00.Translation.Infrastructure.Interfaces;

namespace MSG00.Translation.Infrastructure.Reader
{
    internal class CsvbReader : ICsvbReader
    {
        private static readonly List<byte> EMPTY_BYTES = new() { 0x00, 0x00, 0x00, 0x00 };
        private static readonly List<byte> MAPI_BYTES = new() { 0x4D, 0x41, 0x50, 0x49 };
        private static readonly List<byte> BLOCK_BYTES = new() { 0x42, 0x4C, 0x4F, 0x43, 0x4B, 0x31, 0x00, 0x00 };

        /// <summary>
        /// Reads the type of the csvb file; ex: <see cref="CsvbFileType.MAPI"/>, <see cref="CsvbFileType.BLOCK"/>, <see cref="CsvbFileType.EVM_BASE"/>
        /// </summary>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static async Task<CsvbFileType> ReadCsvbFileTypeAsync(Stream fileStream, CancellationToken cancellationToken = default)
        {
            List<byte> data = new List<byte>();
            var currentBytes = new byte[4];

            fileStream.Seek(0x20, SeekOrigin.Begin);
            await fileStream.ReadExactlyAsync(currentBytes, 0x00, 4, cancellationToken).ConfigureAwait(false);

            while (!currentBytes.SequenceEqual(EMPTY_BYTES))
            {
                data.AddRange(currentBytes);
                await fileStream.ReadExactlyAsync(currentBytes, 0x00, 4, cancellationToken).ConfigureAwait(false);
            }

            return data switch
            {
                List<byte> actual when actual.SequenceEqual(EvmBaseHeaderConst.EVM_BASE_BYTES) => CsvbFileType.EVM_BASE,
                List<byte> actual when actual.SequenceEqual(MAPI_BYTES) => CsvbFileType.MAPI,
                List<byte> actual when actual.SequenceEqual(BLOCK_BYTES) => CsvbFileType.BLOCK,
                _ => throw new NotImplementedException(),
            };
        }

        /// <summary>
        /// Reads the full csvb header; first 16 bytes
        /// </summary>
        /// <param name="fileStream"></param>
        /// <returns></returns>
        private static async Task<CsvbHeader> ReadFileHeaderAsync(Stream fileStream, CancellationToken cancellationToken = default)
        {
            fileStream.Seek(0, SeekOrigin.Begin);
            var staticFirstHeaderBytes = new byte[0x0C];
            await fileStream.ReadExactlyAsync(staticFirstHeaderBytes, 0, 12, cancellationToken).ConfigureAwait(false);

            var fileOffsetToAreaBetweenPointerAndTextTableBytes = new byte[4];
            await fileStream.ReadExactlyAsync(fileOffsetToAreaBetweenPointerAndTextTableBytes, 0, 4, cancellationToken).ConfigureAwait(false);

            var fileOffsetToTextTableBytes = new byte[4];
            await fileStream.ReadExactlyAsync(fileOffsetToTextTableBytes, 0, 4, cancellationToken).ConfigureAwait(false);

            var fileOffsetToAreaBetweenTextAndOffsetTableBytes = new byte[4];
            await fileStream.ReadExactlyAsync(fileOffsetToAreaBetweenTextAndOffsetTableBytes, 0, 4, cancellationToken).ConfigureAwait(false);

            var fileOffsetToOffsetTableBytes = new byte[4];
            await fileStream.ReadExactlyAsync(fileOffsetToOffsetTableBytes, 0, 4, cancellationToken).ConfigureAwait(false);

            return new CsvbHeader()
            {
                StaticFirstHeaderBytes = staticFirstHeaderBytes,
                FileOffsetToAreaBetweenPointerAndTextTable = fileOffsetToAreaBetweenPointerAndTextTableBytes,
                FileOffsetToTextTable = fileOffsetToTextTableBytes,
                FileOffsetToAreaBetweenTextAndOffsetTable = fileOffsetToAreaBetweenTextAndOffsetTableBytes,
                FileOffsetToOffsetTable = fileOffsetToOffsetTableBytes
            };
        }

        public virtual async Task<CsvbFile> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var header = await ReadFileHeaderAsync(stream, cancellationToken);
            var csvbType = await ReadCsvbFileTypeAsync(stream, cancellationToken);

            return new CsvbFile()
            {
                Type = csvbType,
                Header = header
            };
        }
    }
}
