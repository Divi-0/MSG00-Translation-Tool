using MSG00.Translation.Domain.EvmBase;
using MSG00.Translation.Infrastructure.Domain.Shared;
using System.Collections.ObjectModel;

namespace MSG00.Translation.Infrastructure.Reader.EvmBase
{
    internal class EvmReader : CsvbMapiReader, IEvmReader
    {
        public async Task<EvmBaseCsvb> ReadAsync(Stream stream)
        {
            //byte[] fileBytes = new byte[stream.Length];

            //await stream.ReadAsync(fileBytes).ConfigureAwait(false);

            //int fileSizeToTextEnd = GetFileSizeUntilEndOfTextTable(fileBytes);
            //int fileSizeWithUnimportantInfo = GetFileSizeWithUnimportantBytes(fileBytes);

            //EvmCsvb evmCsvb = new EvmCsvb()
            //{
            //    FileOffsetToAreaBetweenPointerAndTextTable = GetFileOffsetToAreaBetweenPointerAndTextTable(fileBytes),
            //    FullHeaderSize = GetFullHeaderSize(fileBytes),
            //    HeaderBytes = await ReadFileHeader(stream).ConfigureAwait(false),
            //    FileOffsetToOffsetTable = fileSizeWithUnimportantInfo,
            //    FileOffsetToTextTable = BitConverter.ToInt32(new byte[] { fileBytes[16], fileBytes[17], fileBytes[18], fileBytes[19] })
            //};

            //var pointers = new List<EvmPointer>();
            //try
            //{
            //    for (int i = 0x58; i < evmCsvb.FileOffsetToAreaBetweenPointerAndTextTable; i += 8)
            //    {
            //        //Pointer Table
            //        byte[] pointerBytes = new byte[4];
            //        Array.Copy(fileBytes, i, pointerBytes, 0, 4);
            //        int pointerValue = BitConverter.ToInt32(pointerBytes);

            //        byte[] typeBytes = new byte[4];
            //        Array.Copy(fileBytes, i + 4, typeBytes, 0, 4);
            //        EvmPointerType pointerType = (EvmPointerType)BitConverter.ToInt32(typeBytes);

            //        //Offset Table
            //        byte[] offsetPointerTypeBytes = new byte[4];
            //        Array.Copy(fileBytes, evmCsvb.FileOffsetToOffsetTable + pointerValue, offsetPointerTypeBytes, 0, 4);
            //        int offsetPointerType = BitConverter.ToInt32(offsetPointerTypeBytes);

            //        byte[] offsetPointerAttributeBytes = new byte[4];
            //        Array.Copy(fileBytes, evmCsvb.FileOffsetToOffsetTable + pointerValue, offsetPointerAttributeBytes, 0, 4);
            //        int offsetPointerAttribute = BitConverter.ToInt32(offsetPointerAttributeBytes);

            //        byte[] offsetPointerBytes = new byte[4];
            //        int offsetPointerValue;
            //        try
            //        {
            //            Array.Copy(fileBytes, evmCsvb.FileOffsetToOffsetTable + pointerValue + 4, offsetPointerBytes, 0, 4);
            //            offsetPointerValue = BitConverter.ToInt32(offsetPointerBytes);
            //        }
            //        catch (Exception e)
            //        {

            //            throw e;
            //        }

            //        //Schauen wie viele hex values ein pointer type im offset table hat z.B. 67 hat 3 => 67 00 00 00 03 00 00 00 53 01 00 00

            //        EvmPointer? currentPointer = null;

            //        switch (pointerType)
            //        {
            //            case EvmPointerType.Text:
            //                currentPointer = new EvmTextPointer
            //                {
            //                    Type = EvmPointerType.Text,
            //                    OffsetValue = offsetPointerValue,
            //                    OffsetPointerType = offsetPointerType,
            //                    OffsetPointerAttribute = offsetPointerAttribute,
            //                    TextBox = GetTextFromFile(evmCsvb, fileBytes, offsetPointerValue)
            //                };
            //                break;
            //            default:
            //                currentPointer = new EvmPointer
            //                {
            //                    Type = EvmPointerType.Unknown,
            //                    OffsetValue = offsetPointerValue,
            //                    OffsetPointerType = offsetPointerType,
            //                    OffsetPointerAttribute = offsetPointerAttribute
            //                };
            //                break;
            //        }

            //        pointers.Add(currentPointer);
            //    }
            //}
            //catch (Exception e)
            //{

            //    throw e;
            //}

            //return evmCsvb;
            return null;
        }

        private ObservableCollection<CsvbTextLine> GetTextFromFile(EvmBaseCsvb evmCsvb, byte[] fileBytes, int offset)
        {
            ObservableCollection<CsvbTextLine> conversationTextLines = new ObservableCollection<CsvbTextLine>();

            List<byte> textBytes = new List<byte>();
            for (int i = evmCsvb.FileOffsetToTextTable + offset; fileBytes[i] != 0x00; i++)
            {
                if (fileBytes[i] == 0x0A)
                {
                    conversationTextLines.Add(new CsvbTextLine
                    {
                        Text = _shiftJISEncoding.GetString(textBytes.ToArray())
                    });

                    textBytes.Clear();
                    continue;
                }

                textBytes.Add(fileBytes[i]);
            }

            //add last line
            conversationTextLines.Add(new CsvbTextLine
            {
                Text = _shiftJISEncoding.GetString(textBytes.ToArray())
            });

            return conversationTextLines;
        }
    }
}