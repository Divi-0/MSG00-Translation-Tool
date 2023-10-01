using MSG00.Translation.Infrastructure.Domain.Prologue;
using MSG00.Translation.Infrastructure.Domain.Prologue.Enums;

namespace MSG00.Translation.Infrastructure.Reader.Prologue
{
    internal sealed class PrologueReader : CsvbMapiReader, IPrologueReader
    {
        public async Task<PrologueCsvb> ReadFile(Stream stream)
        {
            try
            {
                byte[] fileBytes = new byte[stream.Length];

                await stream.ReadAsync(fileBytes).ConfigureAwait(false);

                int fileSizeToTextEnd = GetFileSizeUntilEndOfTextTable(fileBytes);
                int fileSizeWithUnimportantInfo = GetFileSizeWithUnimportantBytes(fileBytes);

                PrologueCsvb proEpilogueCsvb = new PrologueCsvb()
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
                for (int i = 0x50; i < proEpilogueCsvb.FileOffsetToAreaBetweenPointerAndTextTable; i += 8)
                {
                    byte[] pointerBytes = new byte[4];
                    Array.Copy(fileBytes, i, pointerBytes, 0, 4);
                    int pointer = BitConverter.ToInt32(pointerBytes);

                    byte[] typeBytes = new byte[4];
                    Array.Copy(fileBytes, i + 4, typeBytes, 0, 4);
                    ProloguePointerType type = (ProloguePointerType)BitConverter.ToInt32(typeBytes);

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
                        case ProloguePointerType.StaticOffsetBeforeTextOffset:
                        case ProloguePointerType.StaticOffsetAfterImage:
                        case ProloguePointerType.StartOfEndReference:
                        case ProloguePointerType.EndReferenceSub2:
                        case ProloguePointerType.EndOfEndReference:
                        case ProloguePointerType.Unknown2:
                            AddPointer(new ProloguePointer
                            {
                                Type = type,
                                OffsetValue = BitConverter.ToInt64(GetOffsetBytes(fileBytes, proEpilogueCsvb, pointer, 8)),
                            }, isSamePointer);
                            break;
                        case ProloguePointerType.StartPointer:
                        case ProloguePointerType.Image:
                        case ProloguePointerType.Unknown3:
                            AddPointer(new ProloguePointer
                            {
                                Type = type,
                                OffsetValue = BitConverter.ToInt32(GetOffsetBytes(fileBytes, proEpilogueCsvb, pointer, 4)),
                            }, isSamePointer);
                            break;
                        case ProloguePointerType.Text:
                            byte[] textOffsetBytes = GetOffsetBytes(fileBytes, proEpilogueCsvb, pointer, 8);

                            ProloguePointerText prologuePointerText = new ProloguePointerText
                            {
                                Type = type,
                                OffsetValue = BitConverter.ToInt64(textOffsetBytes)
                            };

                            prologuePointerText.TextLines = GetSingleTextFromFile(proEpilogueCsvb, fileBytes, BitConverter.ToInt32(new byte[] { textOffsetBytes[0], textOffsetBytes[1], textOffsetBytes[2], textOffsetBytes[3] }));

                            AddPointer(prologuePointerText, isSamePointer);
                            break;
                        case ProloguePointerType.TextObjectReference:
                        case ProloguePointerType.EndReferenceSub1:
                        case ProloguePointerType.Unknown1:
                            ProloguePointerTextObjectReference proEpiloguePointerTextObjectReference = new ProloguePointerTextObjectReference
                            {
                                Type = type,
                                OffsetValue = BitConverter.ToInt64(GetOffsetBytes(fileBytes, proEpilogueCsvb, pointer, 8))
                            };

                            if (proEpiloguePointerTextObjectReference.OffsetValue == -1)
                            {
                                AddPointer(proEpiloguePointerTextObjectReference, isSamePointer);
                                break;
                            }

                            byte[] objectReferenceOneOffsetValueBytes = new byte[4];
                            Array.Copy(fileBytes, proEpilogueCsvb.FileSizeWithUnimportantInfo + pointer, objectReferenceOneOffsetValueBytes, 0, 4);
                            int objectReferenceOneOffsetValue = BitConverter.ToInt32(objectReferenceOneOffsetValueBytes);

                            byte[] objectReferenceTwoOffsetValueBytes = new byte[4];
                            Array.Copy(fileBytes, proEpilogueCsvb.FileSizeWithUnimportantInfo + pointer + 4, objectReferenceTwoOffsetValueBytes, 0, 4);
                            int objectReferenceTwoOffsetValue = BitConverter.ToInt32(objectReferenceTwoOffsetValueBytes);

                            proEpiloguePointerTextObjectReference.TextReferences.Add(GetSingleTextFromFile(proEpilogueCsvb, fileBytes, objectReferenceOneOffsetValue)[0].Text);

                            if (objectReferenceOneOffsetValue != objectReferenceTwoOffsetValue)
                            {
                                proEpiloguePointerTextObjectReference.TextReferences.Add(GetSingleTextFromFile(proEpilogueCsvb, fileBytes, objectReferenceTwoOffsetValue)[0].Text);
                            }

                            AddPointer(proEpiloguePointerTextObjectReference, isSamePointer);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(type));
                    }

                    lastPointer = pointer;
                }

                return proEpilogueCsvb;

                void AddPointer(ProloguePointer proEpiloguePointer, bool isSamePointer)
                {
                    if (isSamePointer)
                    {
                        proEpilogueCsvb.ProEpiloguePointers.Last().SubPointer.Add(proEpiloguePointer);
                        return;
                    }

                    proEpilogueCsvb.ProEpiloguePointers.Add(proEpiloguePointer);
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private static byte[] GetOffsetBytes(byte[] fileBytes, PrologueCsvb proEpilogueCsvb, int pointer, int offsetLength)
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