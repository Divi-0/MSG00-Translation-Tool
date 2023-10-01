using MSG00.Translation.Infrastructure.Domain.Epilogue;
using MSG00.Translation.Infrastructure.Domain.Epilogue.Enums;

namespace MSG00.Translation.Infrastructure.Reader.Epilogue
{
    internal class EpilogueReader : CsvbMapiReader, IEpilogueReader
    {
        public async Task<EpilogueCsvb> ReadFile(Stream stream)
        {
            try
            {
                byte[] fileBytes = new byte[stream.Length];

                await stream.ReadAsync(fileBytes).ConfigureAwait(false);

                int fileSizeToTextEnd = GetFileSizeUntilEndOfTextTable(fileBytes);
                int fileSizeWithUnimportantInfo = GetFileSizeWithUnimportantBytes(fileBytes);

                EpilogueCsvb epilogueCsvb = new EpilogueCsvb()
                {
                    FileOffsetToAreaBetweenPointerAndTextTable = GetFileOffsetToAreaBetweenPointerAndTextTable(fileBytes),
                    FullHeaderSize = GetFullHeaderSize(fileBytes),
                    FileSizeToTextEnd = fileSizeToTextEnd,
                    FileSizeWithUnimportantInfo = fileSizeWithUnimportantInfo,
                    CountOfPointersInFile = fileBytes[52],
                    HeaderBytes = await ReadFileHeader(stream).ConfigureAwait(false),
                    MapiHeaderBytes = await GetMapiHeader(stream).ConfigureAwait(false),
                    AfterTextSectionBytes = await GetConstBytesAfterTextSection(stream, fileSizeToTextEnd, fileSizeWithUnimportantInfo).ConfigureAwait(false),
                };

                int lastPointer = -1;
                bool isSamePointer = false;
                for (int i = 0x50; i < epilogueCsvb.FileOffsetToAreaBetweenPointerAndTextTable; i += 8)
                {
                    byte[] pointerBytes = new byte[4];
                    Array.Copy(fileBytes, i, pointerBytes, 0, 4);
                    int pointer = BitConverter.ToInt32(pointerBytes);

                    byte[] typeBytes = new byte[4];
                    Array.Copy(fileBytes, i + 4, typeBytes, 0, 4);
                    EpiloguePointerType type = (EpiloguePointerType)BitConverter.ToInt32(typeBytes);

                    if (pointer == lastPointer)
                    {
                        isSamePointer = true;
                    }
                    else
                    {
                        isSamePointer = false;
                    }

                    switch (type)
                    {
                        case EpiloguePointerType.StaticOffsetBeforeTextOffset:
                        case EpiloguePointerType.StaticOffsetAfterImage:
                        case EpiloguePointerType.EndReferenceSub2:
                        case EpiloguePointerType.Unknown2:
                            AddPointer(new EpiloguePointer
                            {
                                Type = type,
                                OffsetValue = BitConverter.ToInt64(GetOffsetBytes(fileBytes, epilogueCsvb, pointer, 8)),
                            }, isSamePointer);
                            break;
                        case EpiloguePointerType.StartPointer:
                        case EpiloguePointerType.Image:
                        case EpiloguePointerType.Unknown3:
                            AddPointer(new EpiloguePointer
                            {
                                Type = type,
                                OffsetValue = BitConverter.ToInt32(GetOffsetBytes(fileBytes, epilogueCsvb, pointer, 4)),
                            }, isSamePointer);
                            break;
                        case EpiloguePointerType.Text:
                            byte[] textOffsetBytes = GetOffsetBytes(fileBytes, epilogueCsvb, pointer, 8);

                            EpiloguePointerText prologuePointerText = new EpiloguePointerText
                            {
                                Type = type,
                                OffsetValue = BitConverter.ToInt64(textOffsetBytes)
                            };

                            prologuePointerText.TextLines = GetSingleTextFromFile(epilogueCsvb, fileBytes, BitConverter.ToInt32(new byte[] { textOffsetBytes[0], textOffsetBytes[1], textOffsetBytes[2], textOffsetBytes[3] }));

                            AddPointer(prologuePointerText, isSamePointer);
                            break;
                        case EpiloguePointerType.TextObjectReference:
                        case EpiloguePointerType.EndReferenceSub1:
                        case EpiloguePointerType.Unknown1:
                        case EpiloguePointerType.StartOfEndReference:
                            EpiloguePointerTextObjectReference proEpiloguePointerTextObjectReference = new EpiloguePointerTextObjectReference
                            {
                                Type = type,
                                OffsetValue = BitConverter.ToInt64(GetOffsetBytes(fileBytes, epilogueCsvb, pointer, 8))
                            };

                            if (proEpiloguePointerTextObjectReference.OffsetValue == -1)
                            {
                                AddPointer(proEpiloguePointerTextObjectReference, isSamePointer);
                                break;
                            }

                            byte[] objectReferenceOneOffsetValueBytes = new byte[4];
                            Array.Copy(fileBytes, epilogueCsvb.FileSizeWithUnimportantInfo + pointer, objectReferenceOneOffsetValueBytes, 0, 4);
                            int objectReferenceOneOffsetValue = BitConverter.ToInt32(objectReferenceOneOffsetValueBytes);

                            byte[] objectReferenceTwoOffsetValueBytes = new byte[4];
                            Array.Copy(fileBytes, epilogueCsvb.FileSizeWithUnimportantInfo + pointer + 4, objectReferenceTwoOffsetValueBytes, 0, 4);
                            int objectReferenceTwoOffsetValue = BitConverter.ToInt32(objectReferenceTwoOffsetValueBytes);

                            proEpiloguePointerTextObjectReference.TextReferences.Add(GetSingleTextFromFile(epilogueCsvb, fileBytes, objectReferenceOneOffsetValue)[0].Text);

                            if (objectReferenceOneOffsetValue != objectReferenceTwoOffsetValue)
                            {
                                proEpiloguePointerTextObjectReference.TextReferences.Add(GetSingleTextFromFile(epilogueCsvb, fileBytes, objectReferenceTwoOffsetValue)[0].Text);
                            }

                            AddPointer(proEpiloguePointerTextObjectReference, isSamePointer);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(type));
                    }

                    lastPointer = pointer;
                }

                return epilogueCsvb;

                void AddPointer(EpiloguePointer epiloguePointer, bool isSamePointer)
                {
                    if (isSamePointer)
                    {
                        epilogueCsvb.EpiloguePointers.Last().SubPointer.Add(epiloguePointer);
                        return;
                    }

                    epilogueCsvb.EpiloguePointers.Add(epiloguePointer);
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private static byte[] GetOffsetBytes(byte[] fileBytes, EpilogueCsvb proEpilogueCsvb, int pointer, int offsetLength)
        {
            if (fileBytes.Length < proEpilogueCsvb.FileSizeWithUnimportantInfo + pointer + offsetLength)
            {
                return BitConverter.GetBytes(offsetLength == 8 ? (long)-1 : -1);
            }

            byte[] offsetBytes = new byte[offsetLength];
            Array.Copy(fileBytes, proEpilogueCsvb.FileSizeWithUnimportantInfo + pointer, offsetBytes, 0, offsetLength);
            return offsetBytes;
        }
    }
}
