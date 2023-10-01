using MSG00.Translation.Infrastructure.Domain.Epilogue;
using MSG00.Translation.Infrastructure.Domain.Epilogue.Enums;
using MSG00.Translation.Infrastructure.Domain.Shared;
using MSG00.Translation.Infrastructure.Extensions;

namespace MSG00.Translation.Infrastructure.Writer.Epilogue
{
    internal class EpilogueWriter : CsvbWriter, IEpilogueWriter
    {
        public async Task WriteFile(Stream stream, EpilogueCsvb epilogueCsvb)
        {
            try
            {
                CalculateOffsets(epilogueCsvb.EpiloguePointers);

                await ClearFile(stream).ConfigureAwait(false);

                await WriteHeader(stream, epilogueCsvb).ConfigureAwait(false);
                await WritePointerTable(stream, epilogueCsvb).ConfigureAwait(false);
                await WriteTextTable(stream, epilogueCsvb).ConfigureAwait(false);

                int newFileSizeToTextEnd = Convert.ToInt32(stream.Length);

                await stream.WriteAsync(epilogueCsvb.AfterTextSectionBytes).ConfigureAwait(false);

                int newFileSizeToOffsetTable = Convert.ToInt32(stream.Length);

                await WriteOffsetTable(stream, epilogueCsvb);

                await OverrideHeader(stream, newFileSizeToTextEnd, newFileSizeToOffsetTable).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static async Task ClearFile(Stream stream)
        {
            stream.SetLength(0);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        private static async Task WriteHeader(Stream stream, EpilogueCsvb epilogueCsvb)
        {
            await stream.WriteAsync(epilogueCsvb.HeaderBytes).ConfigureAwait(false);
            await stream.WriteAsync(epilogueCsvb.MapiHeaderBytes).ConfigureAwait(false);
        }

        private static async Task WritePointerTable(Stream stream, EpilogueCsvb epilogueCsvb)
        {
            int currentPointer = 0;
            foreach (EpiloguePointer proEpiloguePointer in epilogueCsvb.EpiloguePointers)
            {
                await stream.WriteAsync(BitConverter.GetBytes(currentPointer)).ConfigureAwait(false);
                await stream.WriteAsync(BitConverter.GetBytes((int)proEpiloguePointer.Type)).ConfigureAwait(false);

                if (proEpiloguePointer.SubPointer.Any())
                {
                    foreach (EpiloguePointer subPointer in proEpiloguePointer.SubPointer)
                    {
                        await stream.WriteAsync(BitConverter.GetBytes(currentPointer)).ConfigureAwait(false);
                        await stream.WriteAsync(BitConverter.GetBytes((int)subPointer.Type)).ConfigureAwait(false);
                    }
                }

                switch (proEpiloguePointer.Type)
                {
                    case EpiloguePointerType.Image:
                    case EpiloguePointerType.StartPointer:
                    case EpiloguePointerType.Unknown3:
                        currentPointer += 4;
                        break;
                    default:
                        currentPointer += 8;
                        break;
                }
            }

            await stream.WriteAsync(new byte[] { 2, 0, 0, 0 }).ConfigureAwait(false);
            await stream.WriteAsync(new byte[] { 2, 0, 0, 0 }).ConfigureAwait(false);
        }

        private async Task WriteTextTable(Stream stream, EpilogueCsvb epilogueCsvb)
        {
            await stream.WriteAsync(new byte[] { 0, 0, 0, 0 }).ConfigureAwait(false);

            Dictionary<string, bool> objectReferencesInText = new Dictionary<string, bool>();

            await WriteTextToTextTable(stream, epilogueCsvb.EpiloguePointers, objectReferencesInText).ConfigureAwait(false);
        }

        private async Task WriteTextToTextTable(Stream stream, List<EpiloguePointer> epiloguePointers, Dictionary<string, bool> objectReferencesInText)
        {
            foreach (EpiloguePointer epiloguePointer in epiloguePointers)
            {
                switch (epiloguePointer.Type)
                {
                    case EpiloguePointerType.TextObjectReference:
                    case EpiloguePointerType.EndReferenceSub1:
                    case EpiloguePointerType.Unknown1:
                        EpiloguePointerTextObjectReference textObjectReference = (EpiloguePointerTextObjectReference)epiloguePointer;

                        foreach (string referenceObj in textObjectReference.TextReferences)
                        {
                            if (objectReferencesInText.ContainsKey(referenceObj))
                            {
                                continue;
                            }

                            byte[] newTextBytes = _shiftJISEncoding.GetBytes(referenceObj).AddTrailingCsvbFileZeroes();

                            await stream.WriteAsync(newTextBytes).ConfigureAwait(false);

                            objectReferencesInText.Add(referenceObj, true);
                        }
                        break;
                    case EpiloguePointerType.Text:
                        EpiloguePointerText epiloguePointerText = (EpiloguePointerText)epiloguePointer;

                        List<byte> fullTextBytes = new List<byte>();
                        foreach (CsvbTextLine csvbTextLine in epiloguePointerText.TextLines)
                        {
                            if (fullTextBytes.Any())
                            {
                                fullTextBytes.Add(0x0A);
                            }

                            fullTextBytes.AddRange(_shiftJISEncoding.GetBytes(csvbTextLine.MutableText));
                        }

                        byte[] newTextbytes = fullTextBytes.ToArray().AddTrailingCsvbFileZeroes();

                        await stream.WriteAsync(newTextbytes).ConfigureAwait(false);
                        break;
                }

                await WriteTextToTextTable(stream, epiloguePointer.SubPointer, objectReferencesInText).ConfigureAwait(false);
            }
        }

        private async Task WriteOffsetTable(Stream stream, EpilogueCsvb epilogueCsvb)
        {
            await WriteOffsetToTable(stream, epilogueCsvb.EpiloguePointers).ConfigureAwait(false);
        }

        private async Task WriteOffsetToTable(Stream stream, List<EpiloguePointer> epiloguePointers)
        {
            foreach (EpiloguePointer epiloguePointer in epiloguePointers)
            {
                if (epiloguePointer.OffsetValue == -1)
                {
                    continue;
                }

                switch (epiloguePointer.Type)
                {
                    case EpiloguePointerType.StartPointer:
                    case EpiloguePointerType.Image:
                    case EpiloguePointerType.Unknown3:
                        await stream.WriteAsync(BitConverter.GetBytes(Convert.ToInt32(epiloguePointer.OffsetValue))).ConfigureAwait(false);
                        break;
                    default:
                        await stream.WriteAsync(BitConverter.GetBytes(epiloguePointer.OffsetValue)).ConfigureAwait(false);
                        break;
                }

                //no subpointers
            }
        }

        private static async Task OverrideHeader(Stream stream, int newFileSizeToEndOfText, int newFileSizeToStartOfOffsetTable)
        {
            stream.Seek(20, SeekOrigin.Begin);
            await stream.WriteAsync(BitConverter.GetBytes(newFileSizeToEndOfText)).ConfigureAwait(false);

            stream.Seek(24, SeekOrigin.Begin);
            await stream.WriteAsync(BitConverter.GetBytes(newFileSizeToStartOfOffsetTable)).ConfigureAwait(false);
        }

        private int CalculateOffsets(List<EpiloguePointer> epiloguePointers, Dictionary<string, int>? objectReferencesInText = null, int currentOffsetValue = 4)
        {
            if (objectReferencesInText == null)
            {
                objectReferencesInText = new Dictionary<string, int>();
            }

            foreach (EpiloguePointer epiloguePointer in epiloguePointers)
            {
                switch (epiloguePointer.Type)
                {
                    case EpiloguePointerType.TextObjectReference:
                    case EpiloguePointerType.EndReferenceSub1:
                    case EpiloguePointerType.Unknown1:
                    case EpiloguePointerType.StartOfEndReference:
                        if (epiloguePointer.OffsetValue == -1)
                        {
                            break;
                        }

                        EpiloguePointerTextObjectReference textObjectReference = (EpiloguePointerTextObjectReference)epiloguePointer;

                        List<byte> offsetBytes = new List<byte>();
                        foreach (string referenceObj in textObjectReference.TextReferences)
                        {
                            if (objectReferencesInText.TryGetValue(referenceObj, out int offsetValue))
                            {
                                offsetBytes.AddRange(BitConverter.GetBytes(offsetValue));
                                continue;
                            }

                            offsetBytes.AddRange(BitConverter.GetBytes(currentOffsetValue));

                            objectReferencesInText.Add(referenceObj, currentOffsetValue);

                            currentOffsetValue += _shiftJISEncoding.GetBytes(referenceObj).AddTrailingCsvbFileZeroes().Length;
                        }

                        if (!offsetBytes.Any())
                        {
                            offsetBytes.AddRange(BitConverter.GetBytes(currentOffsetValue));
                            offsetBytes.AddRange(BitConverter.GetBytes(currentOffsetValue));
                        }

                        if (offsetBytes.Count == 4)
                        {
                            offsetBytes.AddRange(offsetBytes);
                        }

                        textObjectReference.OffsetValue = BitConverter.ToInt64(offsetBytes.ToArray());
                        break;
                    case EpiloguePointerType.Text:
                        EpiloguePointerText epiloguePointerText = (EpiloguePointerText)epiloguePointer;

                        List<byte> fullTextBytes = new List<byte>();
                        foreach (CsvbTextLine csvbTextLine in epiloguePointerText.TextLines)
                        {
                            if (fullTextBytes.Any())
                            {
                                fullTextBytes.Add(0x0A);
                            }

                            fullTextBytes.AddRange(_shiftJISEncoding.GetBytes(csvbTextLine.MutableText));
                        }

                        List<byte> textOffsetBytes = new List<byte>();
                        textOffsetBytes.AddRange(BitConverter.GetBytes(currentOffsetValue));

                        byte[] additionalBytes = new byte[4];
                        Array.Copy(BitConverter.GetBytes(epiloguePointerText.OffsetValue), 4, additionalBytes, 0, 4);
                        textOffsetBytes.AddRange(additionalBytes);


                        epiloguePointerText.OffsetValue = BitConverter.ToInt64(textOffsetBytes.ToArray());
                        currentOffsetValue += fullTextBytes.ToArray().AddTrailingCsvbFileZeroes().Length;
                        break;
                }

                currentOffsetValue = CalculateOffsets(epiloguePointer.SubPointer, objectReferencesInText, currentOffsetValue);
            }

            return currentOffsetValue;
        }
    }
}
