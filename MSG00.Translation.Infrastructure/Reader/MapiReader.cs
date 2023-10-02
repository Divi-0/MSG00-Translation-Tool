using MSG00.Translation.Domain.Files.Csvb;
using MSG00.Translation.Infrastructure.Domain.Shared;
using System.Collections.ObjectModel;
using System.Text;

namespace MSG00.Translation.Infrastructure.Reader
{
    internal abstract class CsvbMapiReader : CsvbReader
    {
        protected readonly Encoding _shiftJISEncoding;

        public CsvbMapiReader()
        {
            _shiftJISEncoding = Encoding.GetEncoding("Shift-JIS");
        }

        protected static async Task<byte[]> GetMapiHeader(Stream fileStream)
        {
            fileStream.Seek(32, SeekOrigin.Begin);

            byte[] bytes = new byte[48];

            await fileStream.ReadExactlyAsync(bytes, 0, 48).ConfigureAwait(false);

            return bytes;
        }

        protected static async Task<byte[]> GetConstBytesAfterTextSection(Stream fileStream, int startOffset, int offsetTableOffset)
        {
            fileStream.Seek(startOffset, SeekOrigin.Begin);

            int lengthOfSection = offsetTableOffset - startOffset;

            byte[] bytes = new byte[lengthOfSection];

            await fileStream.ReadExactlyAsync(bytes, 0, lengthOfSection).ConfigureAwait(false);

            return bytes;
        }

        protected static int GetFileSizeUntilEndOfTextTable(byte[] fileBytes)
        {
            byte[] bytes = new byte[4];
            Array.Copy(fileBytes, 20, bytes, 0, 4);

            return BitConverter.ToInt32(bytes);
        }

        protected static int GetFileSizeWithUnimportantBytes(byte[] fileBytes)
        {
            byte[] bytes = new byte[4];
            Array.Copy(fileBytes, 24, bytes, 0, 4);

            return BitConverter.ToInt32(bytes);
        }

        protected ObservableCollection<CsvbTextLine> GetSingleTextFromFile(CsvbFile csvbFile, byte[] fileBytes, int offset)
        {
            throw new NotImplementedException();
            //ObservableCollection<CsvbTextLine> conversationTextLines = new ObservableCollection<CsvbTextLine>();

            //List<byte> textBytes = new List<byte>();
            //for (int i = csvbFile.FullHeaderSize + offset; fileBytes[i] != 0x00; i++)
            //{
            //    if (fileBytes[i] == 0x0A)
            //    {
            //        conversationTextLines.Add(new CsvbTextLine
            //        {
            //            Text = _shiftJISEncoding.GetString(textBytes.ToArray())
            //        });

            //        textBytes.Clear();
            //        continue;
            //    }

            //    textBytes.Add(fileBytes[i]);
            //}

            ////add last line
            //conversationTextLines.Add(new CsvbTextLine
            //{
            //    Text = _shiftJISEncoding.GetString(textBytes.ToArray())
            //});

            //return conversationTextLines;
        }

        protected static int GetTextByteLengthWithZeros(CsvbFile csvbFile, byte[] fileBytes, int offset)
        {
            throw new NotImplementedException();

            //int originalByteLenth = 0;
            //bool hasReachedZeroBytes = false;
            //for (int i = csvbFile.FullHeaderSize + offset; ; i++)
            //{
            //    if (hasReachedZeroBytes && fileBytes[i] != 0x00)
            //    {
            //        break;
            //    }

            //    if (fileBytes[i] == 0x00)
            //    {
            //        hasReachedZeroBytes = true;
            //    }

            //    originalByteLenth++;
            //}

            //return originalByteLenth;
        }
    }
}
