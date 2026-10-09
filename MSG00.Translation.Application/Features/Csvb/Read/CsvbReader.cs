using System.Text.Json;
using MSG00.Translation.Application.Features.Csvb.EvmBase;
using MSG00.Translation.Application.Features.Csvb;
using MSG00.Translation.Application.Features.Csvb.Mapi;

namespace MSG00.Translation.Application.Features.Csvb.Read
{
    public class CsvbReader : ICsvbReader
    {
        private static readonly List<byte> MapiBytes = new() { 0x4D, 0x41, 0x50, 0x49 };
        private static readonly List<byte> BlockBytes = new() { 0x42, 0x4C, 0x4F, 0x43, 0x4B, 0x31, 0x00, 0x00 };

        /// <summary>
        /// Reads the type of the csvb file; ex: <see cref="CsvbFileType.MAPI"/>, <see cref="CsvbFileType.BLOCK"/>, <see cref="CsvbFileType.EVM_BASE"/>
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static async Task<CsvbFileType> ReadCsvbFileTypeAsync(Stream fileStream, CancellationToken cancellationToken)
        {
            var currentBytes = new byte[8];

            fileStream.Seek(0x20, SeekOrigin.Begin);
            await fileStream.ReadExactlyAsync(currentBytes, 0x00, 8, cancellationToken).ConfigureAwait(false);

            return currentBytes switch
            {
                _ when currentBytes.SequenceEqual(EvmBaseHeaderConst.EVM_BASE_BYTES) => CsvbFileType.EVM_BASE,
                _ when currentBytes.SequenceEqual(MapiCsvb.MapiHeaderDefinitionBytes) => CsvbFileType.MAPI,
                _ when currentBytes.SequenceEqual(BlockBytes) => CsvbFileType.BLOCK,
                _ when currentBytes.AsSpan().StartsWith("OG_"u8) => CsvbFileType.OG_TABLE,
                _ when currentBytes.AsSpan().StartsWith("SFS0_"u8) => CsvbFileType.CUT_IN,
                _ => throw new NotImplementedException($"csbv type {JsonSerializer.Serialize(currentBytes)} not implemented"),
            };
        }

        /// <summary>
        /// Reads the full csvb header; first 16 bytes
        /// </summary>
        private static async Task<CsvbHeader> ReadFileHeaderAsync(Stream fileStream, CancellationToken cancellationToken = default)
        {
            fileStream.Seek(0x0C, SeekOrigin.Begin);

            var fileOffsetToAreaBetweenPointerAndTextTableBytes = new byte[4];
            await fileStream.ReadExactlyAsync(fileOffsetToAreaBetweenPointerAndTextTableBytes, 0, 4, cancellationToken).ConfigureAwait(false);

            var fileOffsetToTextTableBytes = new byte[4];
            await fileStream.ReadExactlyAsync(fileOffsetToTextTableBytes, 0, 4, cancellationToken).ConfigureAwait(false);

            var fileOffsetToAreaBetweenTextAndOffsetTableBytes = new byte[4];
            await fileStream.ReadExactlyAsync(fileOffsetToAreaBetweenTextAndOffsetTableBytes, 0, 4, cancellationToken).ConfigureAwait(false);

            var fileOffsetToOffsetTableBytes = new byte[4];
            await fileStream.ReadExactlyAsync(fileOffsetToOffsetTableBytes, 0, 4, cancellationToken).ConfigureAwait(false);

            return new CsvbHeader
            {
                OffsetToEndOfPointerTable = BitConverter.ToInt32(fileOffsetToAreaBetweenPointerAndTextTableBytes),
                OffsetToEndOfFullHeader = BitConverter.ToInt32(fileOffsetToTextTableBytes),
                OffsetToEndOfTextTable = fileOffsetToAreaBetweenTextAndOffsetTableBytes,
                OffsetToEndOfExtraTable = BitConverter.ToInt32(fileOffsetToOffsetTableBytes)
            };
        }

        public virtual async Task<CsvbFile> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            var header = await ReadFileHeaderAsync(stream, cancellationToken);
            var csvbType = await ReadCsvbFileTypeAsync(stream, cancellationToken);

            return CsvbFile.Create(csvbType, header);
        }
    }
}
