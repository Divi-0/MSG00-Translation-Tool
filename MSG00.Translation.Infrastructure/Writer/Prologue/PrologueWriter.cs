using MSG00.Translation.Infrastructure.Domain.Prologue;
using MSG00.Translation.Infrastructure.Domain.Prologue.Enums;
using MSG00.Translation.Infrastructure.Domain.Shared;
using MSG00.Translation.Infrastructure.Extensions;

namespace MSG00.Translation.Infrastructure.Writer.ProEpilogue
{
    internal class PrologueWriter : CsvbWriter, IPrologueWriter
    {
        public async Task WriteFile(Stream stream, PrologueCsvb proEpilogueCsvb)
        {
            try
            {
                CalculateOffsets(proEpilogueCsvb.ProEpiloguePointers);

                await ClearFile(stream).ConfigureAwait(false);

                await WriteHeader(stream, proEpilogueCsvb).ConfigureAwait(false);
                await WritePointerTable(stream, proEpilogueCsvb).ConfigureAwait(false);
                await WriteTextTable(stream, proEpilogueCsvb).ConfigureAwait(false);

                int newFileSizeToTextEnd = Convert.ToInt32(stream.Length);

                await stream.WriteAsync(proEpilogueCsvb.AfterTextSectionBytes).ConfigureAwait(false);

                int newFileSizeToOffsetTable = Convert.ToInt32(stream.Length);

                await WriteOffsetTable(stream, proEpilogueCsvb);

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

        private static async Task WriteHeader(Stream stream, PrologueCsvb proEpilogueCsvb)
        {
            await stream.WriteAsync(proEpilogueCsvb.HeaderBytes).ConfigureAwait(false);
            await stream.WriteAsync(proEpilogueCsvb.MapiHeaderBytes).ConfigureAwait(false);
        }

        private static async Task WritePointerTable(Stream stream, PrologueCsvb proEpilogueCsvb)
        {
            int currentPointer = 0;
            foreach (ProloguePointer proEpiloguePointer in proEpilogueCsvb.ProEpiloguePointers)
            {
                await stream.WriteAsync(BitConverter.GetBytes(currentPointer)).ConfigureAwait(false);
                await stream.WriteAsync(BitConverter.GetBytes((int)proEpiloguePointer.Type)).ConfigureAwait(false);

                if (proEpiloguePointer.SubPointer.Any())
                {
                    foreach (ProloguePointer subPointer in proEpiloguePointer.SubPointer)
                    {
                        await stream.WriteAsync(BitConverter.GetBytes(currentPointer)).ConfigureAwait(false);
                        await stream.WriteAsync(BitConverter.GetBytes((int)subPointer.Type)).ConfigureAwait(false);
                    }
                }

                switch (proEpiloguePointer.Type)
                {
                    case ProloguePointerType.Image:
                    case ProloguePointerType.StartPointer:
                    case ProloguePointerType.Unknown3:
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

        private async Task WriteTextTable(Stream stream, PrologueCsvb proEpilogueCsvb)
        {
            await stream.WriteAsync(new byte[] { 0, 0, 0, 0 }).ConfigureAwait(false);

            Dictionary<string, bool> objectReferencesInText = new Dictionary<string, bool>();

            await WriteTextToTextTable(stream, proEpilogueCsvb.ProEpiloguePointers, objectReferencesInText).ConfigureAwait(false);
        }

        private async Task WriteTextToTextTable(Stream stream, List<ProloguePointer> proEpiloguePointers, Dictionary<string, bool> objectReferencesInText)
        {
            foreach (ProloguePointer proEpiloguePointer in proEpiloguePointers)
            {
                switch (proEpiloguePointer.Type)
                {
                    case ProloguePointerType.TextObjectReference:
                    case ProloguePointerType.EndReferenceSub1:
                    case ProloguePointerType.Unknown1:
                        ProloguePointerTextObjectReference textObjectReference = (ProloguePointerTextObjectReference)proEpiloguePointer;

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
                    case ProloguePointerType.Text:
                        ProloguePointerText proEpiloguePointerText = (ProloguePointerText)proEpiloguePointer;

                        List<byte> fullTextBytes = new List<byte>();
                        foreach (CsvbTextLine csvbTextLine in proEpiloguePointerText.TextLines)
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

                await WriteTextToTextTable(stream, proEpiloguePointer.SubPointer, objectReferencesInText).ConfigureAwait(false);
            }
        }

        private async Task WriteOffsetTable(Stream stream, PrologueCsvb proEpilogueCsvb)
        {
            await WriteOffsetToTable(stream, proEpilogueCsvb.ProEpiloguePointers).ConfigureAwait(false);
        }

        private async Task WriteOffsetToTable(Stream stream, List<ProloguePointer> proEpiloguePointers)
        {
            foreach (ProloguePointer proEpiloguePointer in proEpiloguePointers)
            {
                switch (proEpiloguePointer.Type)
                {
                    case ProloguePointerType.StartPointer:
                    case ProloguePointerType.Image:
                    case ProloguePointerType.Unknown3:
                        await stream.WriteAsync(BitConverter.GetBytes(Convert.ToInt32(proEpiloguePointer.OffsetValue))).ConfigureAwait(false);
                        break;
                    default:
                        await stream.WriteAsync(BitConverter.GetBytes(proEpiloguePointer.OffsetValue)).ConfigureAwait(false);
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

        private int CalculateOffsets(List<ProloguePointer> proEpiloguePointers, Dictionary<string, int>? objectReferencesInText = null, int currentOffsetValue = 4)
        {
            if (objectReferencesInText == null)
            {
                objectReferencesInText = new Dictionary<string, int>();
            }

            foreach (ProloguePointer proEpiloguePointer in proEpiloguePointers)
            {
                switch (proEpiloguePointer.Type)
                {
                    case ProloguePointerType.TextObjectReference:
                    case ProloguePointerType.EndReferenceSub1:
                    case ProloguePointerType.Unknown1:
                        ProloguePointerTextObjectReference textObjectReference = (ProloguePointerTextObjectReference)proEpiloguePointer;

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
                    case ProloguePointerType.Text:
                        ProloguePointerText proEpiloguePointerText = (ProloguePointerText)proEpiloguePointer;

                        List<byte> fullTextBytes = new List<byte>();
                        foreach (CsvbTextLine csvbTextLine in proEpiloguePointerText.TextLines)
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
                        Array.Copy(BitConverter.GetBytes(proEpiloguePointerText.OffsetValue), 4, additionalBytes, 0, 4);
                        textOffsetBytes.AddRange(additionalBytes);


                        proEpiloguePointerText.OffsetValue = BitConverter.ToInt64(textOffsetBytes.ToArray());
                        currentOffsetValue += fullTextBytes.ToArray().AddTrailingCsvbFileZeroes().Length;
                        break;
                    case ProloguePointerType.StartOfEndReference:
                        if (proEpiloguePointer.OffsetValue == -1)
                        {
                            break;
                        }

                        List<byte> endReferenceOffsetBytes = new List<byte>();
                        endReferenceOffsetBytes.AddRange(BitConverter.GetBytes(currentOffsetValue));
                        endReferenceOffsetBytes.AddRange(BitConverter.GetBytes(currentOffsetValue));

                        proEpiloguePointer.OffsetValue = BitConverter.ToInt64(endReferenceOffsetBytes.ToArray());
                        break;
                }

                currentOffsetValue = CalculateOffsets(proEpiloguePointer.SubPointer, objectReferencesInText, currentOffsetValue);
            }

            return currentOffsetValue;
        }
    }
}
