using MSG00.Translation.Infrastructure.Domain.StaffRoll;
using MSG00.Translation.Infrastructure.Domain.StaffRoll.Enums;

namespace MSG00.Translation.Infrastructure.Reader.StaffRoll
{
    internal class StaffRollReader : CsvbMapiReader, IStaffRollReader
    {
        public async Task<StaffRollCsvb> ReadFile(Stream stream)
        {
            try
            {
                byte[] fileBytes = new byte[stream.Length];

                await stream.ReadAsync(fileBytes).ConfigureAwait(false);

                int fileSizeToTextEnd = GetFileSizeUntilEndOfTextTable(fileBytes);
                int fileSizeWithUnimportantInfo = GetFileSizeWithUnimportantBytes(fileBytes);

                StaffRollCsvb staffRollCsvb = new StaffRollCsvb()
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
                for (int i = 0x50; i < staffRollCsvb.FileOffsetToAreaBetweenPointerAndTextTable; i += 8)
                {
                    byte[] pointerBytes = new byte[4];
                    Array.Copy(fileBytes, i, pointerBytes, 0, 4);
                    int pointer = BitConverter.ToInt32(pointerBytes);

                    byte[] typeBytes = new byte[4];
                    Array.Copy(fileBytes, i + 4, typeBytes, 0, 4);
                    StaffRollPointerType type = (StaffRollPointerType)BitConverter.ToInt32(typeBytes);

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
                        case StaffRollPointerType.StaticOffsetBeforeTextOffset:
                        case StaffRollPointerType.StaticOffsetAfterImage:
                        case StaffRollPointerType.EndReferenceSub2:
                        case StaffRollPointerType.Unknown2:
                            AddPointer(new StaffRollPointer
                            {
                                Type = type,
                                OffsetValue = BitConverter.ToInt64(GetOffsetBytes(fileBytes, staffRollCsvb, pointer, 8)),
                            }, isSamePointer);
                            break;
                        case StaffRollPointerType.StartPointer:
                        case StaffRollPointerType.Image:
                        case StaffRollPointerType.Unknown3:
                            AddPointer(new StaffRollPointer
                            {
                                Type = type,
                                OffsetValue = BitConverter.ToInt32(GetOffsetBytes(fileBytes, staffRollCsvb, pointer, 4)),
                            }, isSamePointer);
                            break;
                        case StaffRollPointerType.Text:
                            byte[] textOffsetBytes = GetOffsetBytes(fileBytes, staffRollCsvb, pointer, 8);

                            StaffRollPointerText prologuePointerText = new StaffRollPointerText
                            {
                                Type = type,
                                OffsetValue = BitConverter.ToInt64(textOffsetBytes)
                            };

                            prologuePointerText.TextLines = GetSingleTextFromFile(staffRollCsvb, fileBytes, BitConverter.ToInt32(new byte[] { textOffsetBytes[0], textOffsetBytes[1], textOffsetBytes[2], textOffsetBytes[3] }));

                            AddPointer(prologuePointerText, isSamePointer);
                            break;
                        case StaffRollPointerType.TextObjectReference:
                        case StaffRollPointerType.EndReferenceSub1:
                        case StaffRollPointerType.Unknown1:
                        case StaffRollPointerType.StartOfEndReference:
                        case StaffRollPointerType.StartUnknown4:
                            StaffRollPointerTextObjectReference proEpiloguePointerTextObjectReference = new StaffRollPointerTextObjectReference
                            {
                                Type = type,
                                OffsetValue = BitConverter.ToInt64(GetOffsetBytes(fileBytes, staffRollCsvb, pointer, 8))
                            };

                            if (proEpiloguePointerTextObjectReference.OffsetValue == -1)
                            {
                                AddPointer(proEpiloguePointerTextObjectReference, isSamePointer);
                                break;
                            }

                            byte[] objectReferenceOneOffsetValueBytes = new byte[4];
                            Array.Copy(fileBytes, staffRollCsvb.FileSizeWithUnimportantInfo + pointer, objectReferenceOneOffsetValueBytes, 0, 4);
                            int objectReferenceOneOffsetValue = BitConverter.ToInt32(objectReferenceOneOffsetValueBytes);

                            byte[] objectReferenceTwoOffsetValueBytes = new byte[4];
                            Array.Copy(fileBytes, staffRollCsvb.FileSizeWithUnimportantInfo + pointer + 4, objectReferenceTwoOffsetValueBytes, 0, 4);
                            int objectReferenceTwoOffsetValue = BitConverter.ToInt32(objectReferenceTwoOffsetValueBytes);

                            proEpiloguePointerTextObjectReference.TextReferences.Add(GetSingleTextFromFile(staffRollCsvb, fileBytes, objectReferenceOneOffsetValue)[0].Text);

                            if (objectReferenceOneOffsetValue != objectReferenceTwoOffsetValue)
                            {
                                proEpiloguePointerTextObjectReference.TextReferences.Add(GetSingleTextFromFile(staffRollCsvb, fileBytes, objectReferenceTwoOffsetValue)[0].Text);
                            }

                            AddPointer(proEpiloguePointerTextObjectReference, isSamePointer);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(type));
                    }

                    lastPointer = pointer;
                }

                return staffRollCsvb;

                void AddPointer(StaffRollPointer staffRollPointer, bool isSamePointer)
                {
                    if (isSamePointer)
                    {
                        staffRollCsvb.Pointers.Last().SubPointer.Add(staffRollPointer);
                        return;
                    }

                    staffRollCsvb.Pointers.Add(staffRollPointer);
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        private static byte[] GetOffsetBytes(byte[] fileBytes, StaffRollCsvb staffRollCsvb, int pointer, int offsetLength)
        {
            if (fileBytes.Length < staffRollCsvb.FileSizeWithUnimportantInfo + pointer + offsetLength)
            {
                return BitConverter.GetBytes(offsetLength == 8 ? (long)-1 : -1);
            }

            byte[] offsetBytes = new byte[offsetLength];
            Array.Copy(fileBytes, staffRollCsvb.FileSizeWithUnimportantInfo + pointer, offsetBytes, 0, offsetLength);
            return offsetBytes;
        }
    }
}
