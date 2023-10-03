using MSG00.Translation.Domain.Csvb;
using MSG00.Translation.Domain.EvmBase;
using MSG00.Translation.Domain.Files.Csvb;
using System.Text;

namespace MSG00.Translation.Infrastructure.Reader.EvmBase
{
    internal class EvmBaseReader : CsvbMapiReader, IEvmBaseReader
    {
        public async Task<EvmBaseCsvb> ReadAsync(CsvbFile csvbFile, Stream stream, CancellationToken cancellationToken = default)
        {
            var evmBaseHeader = await ReadEvmBaseHeaderAsync(stream, cancellationToken);
            var evSeqHeaderBytes = await ReadEvSeqHeaderAsync(stream, evmBaseHeader.FileOffsetToPointerTable, cancellationToken);

            var evmBaseCsvb = new EvmBaseCsvb()
            {
                Type = csvbFile.Type,
                Header = csvbFile.Header,
                EvmBaseHeader = evmBaseHeader,
                EvSeqHeaderBytes = evSeqHeaderBytes
            };

            evmBaseCsvb.PointerAndValue = await ReadPointerAsync(stream, evmBaseCsvb, cancellationToken);

            return evmBaseCsvb;
        }

        #region Header

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
                FileOffsetToPointerTable = BitConverter.ToInt32(fileOffsetToPointerTable)
            };
        }

        private static async Task<byte[]> ReadEvSeqHeaderAsync(Stream stream, long fileOffsetToPointerTable, CancellationToken cancellationToken = default)
        {
            const int startOffset = 0x48;
            stream.Seek(startOffset, SeekOrigin.Begin);

            var count = fileOffsetToPointerTable - startOffset;
            var bytes = new byte[count];
            await stream.ReadExactlyAsync(bytes, 0, Convert.ToInt32(count), cancellationToken);

            return bytes;
        }

        #endregion

        #region Pointer

        private static async Task<List<Pointer>> ReadPointerAsync(Stream stream, EvmBaseCsvb evmBaseCsvb, CancellationToken cancellationToken = default)
        {
            var pointer = new List<Pointer>();

            stream.Seek(evmBaseCsvb.EvmBaseHeader.FileOffsetToPointerTable, SeekOrigin.Begin);

            while (stream.Position < evmBaseCsvb.Header.FileOffsetToAreaBetweenPointerAndTextTable)
            {
                var value = new byte[4];
                await stream.ReadExactlyAsync(value, 0, 4, cancellationToken);

                var type = new byte[4];
                await stream.ReadExactlyAsync(type, 0, 4, cancellationToken);

                pointer.Add(new Pointer
                {
                    Value = BitConverter.ToInt32(value),
                    Type = BitConverter.ToInt32(type)
                });
            }

            await AddOffsetsAsync(stream, evmBaseCsvb, pointer, cancellationToken);
            AddTextToPointers(stream, evmBaseCsvb, pointer);

            return pointer;
        }

        private static async Task AddOffsetsAsync(Stream stream, EvmBaseCsvb evmBaseCsvb, List<Pointer> pointers, CancellationToken cancellationToken = default)
        {
            for (int i = 0; i < pointers.Count; i++)
            {
                stream.Seek(evmBaseCsvb.Header.FileOffsetToOffsetTable + pointers[i].Value, SeekOrigin.Begin);

                var nextOffsetInOffsetTable = pointers.ElementAtOrDefault(i + 1)?.Value ?? stream.Length - evmBaseCsvb.Header.FileOffsetToOffsetTable;
                var count = nextOffsetInOffsetTable - pointers[i].Value;
                var valueBytes = new byte[count];
                await stream.ReadExactlyAsync(valueBytes, 0, Convert.ToInt32(count), cancellationToken);

                pointers[i].Offset = new Offset
                {
                    Value = valueBytes
                };
            }
        }

        private static void AddTextToPointers(Stream stream, EvmBaseCsvb evmBaseCsvb, List<Pointer> pointers)
        {
            foreach (var pointer in pointers)
            {
                ArgumentNullException.ThrowIfNull(pointer.Offset);

                if (pointer.Offset.Value.First() == 0x67)
                {
                    var offsetInTextTable = pointer.Offset.Value[8..12];
                    pointer.Offset.Text = ReadText(stream, evmBaseCsvb, BitConverter.ToInt32(offsetInTextTable));
                }
            }
        }

        private static string ReadText(Stream stream, EvmBaseCsvb evmBaseCsvb, int offsetInTextTable)
        {
            var shiftJisEncoder = Encoding.GetEncoding("Shift-JIS");

            stream.Seek(evmBaseCsvb.Header.FileOffsetToTextTable + offsetInTextTable, SeekOrigin.Begin);

            var currentByte = stream.ReadByte();
            List<byte> bytes = new List<byte>();
            while (currentByte != 0)
            {
                bytes.Add(Convert.ToByte(currentByte));
                currentByte = stream.ReadByte();
            }

            return shiftJisEncoder.GetString(bytes.ToArray());
        }

        #endregion

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