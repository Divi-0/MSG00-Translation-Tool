using MSG00.Translation.Infrastructure.Domain.Shared;
using MSG00.Translation.Infrastructure.Domain.StaffRoll;
using MSG00.Translation.Infrastructure.Domain.StaffRoll.Enums;
using MSG00.Translation.Infrastructure.Extensions;

namespace MSG00.Translation.Infrastructure.Writer.StaffRoll
{
    internal class StaffRollWriter : CsvbWriter, IStaffRollWriter
    {
        public async Task WriteFile(Stream stream, StaffRollCsvb staffRollCsvb)
        {
            try
            {
                CalculateOffsets(staffRollCsvb.Pointers);

                await ClearFile(stream).ConfigureAwait(false);

                await WriteHeader(stream, staffRollCsvb).ConfigureAwait(false);
                await WritePointerTable(stream, staffRollCsvb).ConfigureAwait(false);
                await WriteTextTable(stream, staffRollCsvb).ConfigureAwait(false);

                int newFileSizeToTextEnd = Convert.ToInt32(stream.Length);

                await stream.WriteAsync(staffRollCsvb.AfterTextSectionBytes).ConfigureAwait(false);

                int newFileSizeToOffsetTable = Convert.ToInt32(stream.Length);

                await WriteOffsetTable(stream, staffRollCsvb);

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

        private static async Task WriteHeader(Stream stream, StaffRollCsvb staffRollCsvb)
        {
            await stream.WriteAsync(staffRollCsvb.HeaderBytes).ConfigureAwait(false);
            await stream.WriteAsync(staffRollCsvb.MapiHeaderBytes).ConfigureAwait(false);
        }

        private static async Task WritePointerTable(Stream stream, StaffRollCsvb staffRollCsvb)
        {
            int currentPointer = 4;
            foreach (StaffRollPointer proEpiloguePointer in staffRollCsvb.Pointers)
            {
                await stream.WriteAsync(BitConverter.GetBytes(currentPointer)).ConfigureAwait(false);
                await stream.WriteAsync(BitConverter.GetBytes((int)proEpiloguePointer.Type)).ConfigureAwait(false);

                if (proEpiloguePointer.SubPointer.Any())
                {
                    foreach (StaffRollPointer subPointer in proEpiloguePointer.SubPointer)
                    {
                        await stream.WriteAsync(BitConverter.GetBytes(currentPointer)).ConfigureAwait(false);
                        await stream.WriteAsync(BitConverter.GetBytes((int)subPointer.Type)).ConfigureAwait(false);
                    }
                }

                switch (proEpiloguePointer.Type)
                {
                    case StaffRollPointerType.Image:
                    case StaffRollPointerType.StartPointer:
                    case StaffRollPointerType.Unknown3:
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

        private async Task WriteTextTable(Stream stream, StaffRollCsvb staffRollCsvb)
        {
            Dictionary<string, bool> objectReferencesInText = new Dictionary<string, bool>();

            await WriteTextToTextTable(stream, staffRollCsvb.Pointers, objectReferencesInText).ConfigureAwait(false);
        }

        private async Task WriteTextToTextTable(Stream stream, List<StaffRollPointer> staffRollPointers, Dictionary<string, bool> objectReferencesInText)
        {
            foreach (StaffRollPointer staffollPointer in staffRollPointers)
            {
                switch (staffollPointer.Type)
                {
                    case StaffRollPointerType.TextObjectReference:
                    case StaffRollPointerType.EndReferenceSub1:
                    case StaffRollPointerType.Unknown1:
                    case StaffRollPointerType.StartUnknown4:
                        StaffRollPointerTextObjectReference textObjectReference = (StaffRollPointerTextObjectReference)staffollPointer;

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
                    case StaffRollPointerType.Text:
                        StaffRollPointerText epiloguePointerText = (StaffRollPointerText)staffollPointer;

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

                await WriteTextToTextTable(stream, staffollPointer.SubPointer, objectReferencesInText).ConfigureAwait(false);
            }
        }

        private static async Task WriteOffsetTable(Stream stream, StaffRollCsvb staffRollCsvb)
        {
            await WriteOffsetToTable(stream, staffRollCsvb.Pointers).ConfigureAwait(false);
        }

        private static async Task WriteOffsetToTable(Stream stream, List<StaffRollPointer> staffRollPointers)
        {
            await stream.WriteAsync(BitConverter.GetBytes(int.MaxValue)).ConfigureAwait(false);

            foreach (StaffRollPointer staffRollPointer in staffRollPointers)
            {
                if (staffRollPointer.OffsetValue == -1)
                {
                    continue;
                }

                switch (staffRollPointer.Type)
                {
                    case StaffRollPointerType.StartPointer:
                    case StaffRollPointerType.Image:
                    case StaffRollPointerType.Unknown3:
                        await stream.WriteAsync(BitConverter.GetBytes(Convert.ToInt32(staffRollPointer.OffsetValue))).ConfigureAwait(false);
                        break;
                    case StaffRollPointerType.StartUnknown4:
                        await stream.WriteAsync(BitConverter.GetBytes(1)).ConfigureAwait(false);
                        await stream.WriteAsync(BitConverter.GetBytes(0)).ConfigureAwait(false);
                        break;
                    default:
                        await stream.WriteAsync(BitConverter.GetBytes(staffRollPointer.OffsetValue)).ConfigureAwait(false);
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

        private int CalculateOffsets(List<StaffRollPointer> staffRollPointers, Dictionary<string, int>? objectReferencesInText = null, int currentOffsetValue = 4)
        {
            if (objectReferencesInText == null)
            {
                objectReferencesInText = new Dictionary<string, int>();
            }

            foreach (StaffRollPointer epiloguePointer in staffRollPointers)
            {
                switch (epiloguePointer.Type)
                {
                    case StaffRollPointerType.TextObjectReference:
                    case StaffRollPointerType.EndReferenceSub1:
                    case StaffRollPointerType.Unknown1:
                    case StaffRollPointerType.StartOfEndReference:
                        if (epiloguePointer.OffsetValue == -1)
                        {
                            break;
                        }

                        StaffRollPointerTextObjectReference textObjectReference = (StaffRollPointerTextObjectReference)epiloguePointer;

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
                    case StaffRollPointerType.Text:
                        StaffRollPointerText epiloguePointerText = (StaffRollPointerText)epiloguePointer;

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
