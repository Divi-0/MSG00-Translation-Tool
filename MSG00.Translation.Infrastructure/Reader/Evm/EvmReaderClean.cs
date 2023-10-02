using MSG00.Translation.Domain.Evm;
using MSG00.Translation.Domain.Evm.Enums;
using MSG00.Translation.Infrastructure.Domain.Shared;
using System.Collections.ObjectModel;

namespace MSG00.Translation.Infrastructure.Reader.Evm
{
    internal class EvmReaderClean : CsvbMapiReader, IEvmReader
    {
        public async Task<EvmCsvb> ReadFile(Stream stream)
        {
           

            return null;
        }

        private ObservableCollection<CsvbTextLine> GetTextFromFile(EvmCsvb evmCsvb, byte[] fileBytes, int offset)
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