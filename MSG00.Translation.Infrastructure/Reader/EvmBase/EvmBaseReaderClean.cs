using MSG00.Translation.Domain.EvmBase;
using MSG00.Translation.Domain.Files.Csvb;

namespace MSG00.Translation.Infrastructure.Reader.EvmBase
{
    internal class EvmBaseReaderClean : CsvbMapiReader, IEvmReader
    {
        public async Task<EvmBaseCsvb> ReadAsync(CsvbFile csvbFile, Stream stream, CancellationToken cancellationToken = default)
        {
            var evmBaseHeader = await ReadEvmBaseHeaderAsync(stream, cancellationToken);
            var evSeqHeaderBytes = await ReadEvSeqHeaderAsync(stream, BitConverter.ToInt32(evmBaseHeader.FileOffsetToPointerTable), cancellationToken);

            return null;
        }

        private static async Task<EvmBaseHeader> ReadEvmBaseHeaderAsync(Stream stream, CancellationToken cancellationToken = default)
        {
            stream.Seek(0x28, SeekOrigin.Begin);

            var headerInformation = new byte[28];
            await stream.ReadExactlyAsync(headerInformation, 0, 28, cancellationToken);

            var fileOffsetToPointerTable = new byte[4];
            await stream.ReadExactlyAsync(fileOffsetToPointerTable, 0, 4, cancellationToken);

            return new EvmBaseHeader 
            {
                HeaderInformation = headerInformation,
                FileOffsetToPointerTable = fileOffsetToPointerTable
            };
        }

        private static async Task<byte[]> ReadEvSeqHeaderAsync(Stream stream, int fileOffsetToPointerTable, CancellationToken cancellationToken = default)
        {
            const int startOffset = 0x48;
            stream.Seek(startOffset, SeekOrigin.Begin);

            var count = fileOffsetToPointerTable - startOffset;
            var bytes = new byte[count];
            await stream.ReadExactlyAsync(bytes, 0, count, cancellationToken);

            return bytes;
        }

        //private ObservableCollection<CsvbTextLine> GetTextFromFile(EvmBaseCsvb evmCsvb, byte[] fileBytes, int offset)
        //{
        //    ObservableCollection<CsvbTextLine> conversationTextLines = new ObservableCollection<CsvbTextLine>();

        //    List<byte> textBytes = new List<byte>();
        //    for (int i = evmCsvb.FileOffsetToTextTable + offset; fileBytes[i] != 0x00; i++)
        //    {
        //        if (fileBytes[i] == 0x0A)
        //        {
        //            conversationTextLines.Add(new CsvbTextLine
        //            {
        //                Text = _shiftJISEncoding.GetString(textBytes.ToArray())
        //            });

        //            textBytes.Clear();
        //            continue;
        //        }

        //        textBytes.Add(fileBytes[i]);
        //    }

        //    //add last line
        //    conversationTextLines.Add(new CsvbTextLine
        //    {
        //        Text = _shiftJISEncoding.GetString(textBytes.ToArray())
        //    });

        //    return conversationTextLines;
        //}
    }
}