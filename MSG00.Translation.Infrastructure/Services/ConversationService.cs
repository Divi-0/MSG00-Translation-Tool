using MSG00.Translation.Domain.Files.Csvb;
using MSG00.Translation.Infrastructure.Domain;
using MSG00.Translation.Infrastructure.Domain.Conversation;
using MSG00.Translation.Infrastructure.Domain.Conversation.Enums;
using MSG00.Translation.Infrastructure.Domain.Interfaces;
using MSG00.Translation.Infrastructure.Domain.Shared;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;

namespace MSG00.Translation.Infrastructure.Services
{
    public sealed class ConversationService : IConversationService
    {
        private readonly Encoding _shiftJISEncoding;

        public ConversationService()
        {
            _shiftJISEncoding = Encoding.GetEncoding("Shift-JIS");
        }

        public async Task<ConversationCsvb> GetFile(Stream fileStream)
        {
            throw new NotImplementedException();

            //try
            //{
            //    byte[] fileBytes = new byte[fileStream.Length];

            //    await fileStream.ReadAsync(fileBytes).ConfigureAwait(false);

            //    int fileSizeToTextEnd = int.Parse($"{fileBytes[23]:X2}{fileBytes[22]:X2}{fileBytes[21]:X2}{fileBytes[20]:X2}", System.Globalization.NumberStyles.HexNumber);
            //    int fileSizeWithUnimportantInfo = int.Parse($"{fileBytes[27]:X2}{fileBytes[26]:X2}{fileBytes[25]:X2}{fileBytes[24]:X2}", System.Globalization.NumberStyles.HexNumber);

            //    byte[] csvbHeaderBytes = await GetFileHeader(fileStream).ConfigureAwait(false);
            //    byte[] mapiHeaderBytes = await GetMapiHeader(fileStream).ConfigureAwait(false);
            //    byte[] afterTextSectionBytes = await GetConstBytesAfterTextSection(fileStream, fileSizeToTextEnd, fileSizeWithUnimportantInfo).ConfigureAwait(false);

            //    ConversationCsvb conversationCsvb = new ConversationCsvb()
            //    {
            //        HeaderBytes = csvbHeaderBytes,
            //        MapiHeaderBytes = mapiHeaderBytes,
            //        AfterTextSectionBytes = afterTextSectionBytes,
            //        FileOffsetToAreaBetweenPointerAndTextTable = int.Parse($"{fileBytes[15]:X2}{fileBytes[14]:X2}{fileBytes[13]:X2}{fileBytes[12]:X2}", System.Globalization.NumberStyles.HexNumber),
            //        FullHeaderSize = int.Parse($"{fileBytes[19]:X2}{fileBytes[18]:X2}{fileBytes[17]:X2}{fileBytes[16]:X2}", System.Globalization.NumberStyles.HexNumber),
            //        FileSizeToTextEnd = fileSizeToTextEnd,
            //        FileSizeWithUnimportantInfo = fileSizeWithUnimportantInfo,
            //        CountOfPointersInFile = fileBytes[52]
            //    };

            //    ConversationPointerCharacter? conversationPointer = null;
            //    ConversationPointerCharacter? conversationPointerYesNo = null;
            //    ConversationPointerType? extraType = null;
            //    bool isInsideOfYesNoSection = false;
            //    bool isInYesSection = false;
            //    //0x50 is always the start of the pointers
            //    for (int i = 0x50; i < conversationCsvb.FileOffsetToAreaBetweenPointerAndTextTable; i += 8)
            //    {
            //        int pointer = int.Parse($"{fileBytes[i + 3]:X2}{fileBytes[i + 2]:X2}{fileBytes[i + 1]:X2}{fileBytes[i]:X2}", System.Globalization.NumberStyles.HexNumber);
            //        ConversationPointerType type = (ConversationPointerType)int.Parse($"{fileBytes[i + 7]:X2}{fileBytes[i + 6]:X2}{fileBytes[i + 5]:X2}{fileBytes[i + 4]:X2}", System.Globalization.NumberStyles.HexNumber);

            //        switch (type)
            //        {
            //            case ConversationPointerType.CharacterImage:
            //                NewConversation(pointer, type);
            //                break;
            //            case ConversationPointerType.Text:
            //            case ConversationPointerType.TextWithUnknownOffset:
            //                byte[] textOffsetBytes = new byte[4];
            //                Array.Copy(fileBytes, conversationCsvb.FileSizeWithUnimportantInfo + pointer, textOffsetBytes, 0, 4);

            //                string titleYesNoSection = isInsideOfYesNoSection ? (isInYesSection ? "[Yes]" : "[No]") : string.Empty;

            //                ConversationPointerText conversationPointerText = new ConversationPointerText
            //                {
            //                    Type = type,
            //                    OffsetValue = BitConverter.ToInt32(textOffsetBytes),
            //                    Title = $"{titleYesNoSection} Conversation {conversationCsvb.PointerTable.Count + 1}",
            //                    ItemLifeTime = ConversationItemLifeTime.AlreadyExisted
            //                };

            //                conversationPointerText.Lines = GetSingleTextFromFile(conversationCsvb, fileBytes, conversationPointerText.OffsetValue);
            //                conversationPointerText.OriginalByteLengthWithZeros = GetTextByteLengthWithZeros(conversationCsvb, fileBytes, conversationPointerText.OffsetValue);

            //                if (type == ConversationPointerType.TextWithUnknownOffset)
            //                {
            //                    ConversationPointerTextWithChangingCharImage conversationPointerTextWithUnknownOffset = new ConversationPointerTextWithChangingCharImage(conversationPointerText)
            //                    {
            //                        Type = ConversationPointerType.TextWithUnknownOffset,
            //                        ItemLifeTime = ConversationItemLifeTime.AlreadyExisted
            //                    };

            //                    byte[] unknownOffsetBytes = new byte[12];
            //                    Array.Copy(fileBytes, conversationCsvb.FileSizeWithUnimportantInfo + pointer + 4, unknownOffsetBytes, 0, 12);

            //                    conversationPointerTextWithUnknownOffset.ChangingCharImageOffsetBytes.AddRange(unknownOffsetBytes);

            //                    conversationPointerText = conversationPointerTextWithUnknownOffset;
            //                }

            //                if (isInsideOfYesNoSection)
            //                {
            //                    //if same char image is used from the beginning of the conversation and entering now a yes/no section
            //                    if (conversationPointerYesNo == null)
            //                    {
            //                        conversationPointerYesNo = new ConversationPointerCharacter
            //                        {
            //                            Type = ConversationPointerType.CharacterImage,
            //                            OffsetValue = -1
            //                        };
            //                    }

            //                    conversationPointerYesNo.TextBoxes.Add(conversationPointerText);
            //                    break;
            //                }

            //                conversationPointer!.TextBoxes.Add(conversationPointerText);
            //                break;
            //            case ConversationPointerType.YesNoSelect:
            //                isInsideOfYesNoSection = true;

            //                byte[] textYesNoOffsetBytes = new byte[4];
            //                Array.Copy(fileBytes, conversationCsvb.FileSizeWithUnimportantInfo + pointer, textYesNoOffsetBytes, 0, 4);

            //                ConversationPointerTextWithYesNo conversationPointerTextWithYesNo = new ConversationPointerTextWithYesNo
            //                {
            //                    Type = type,
            //                    OffsetValue = BitConverter.ToInt32(textYesNoOffsetBytes),
            //                    Title = $"[YES/NO] Conversation {conversationCsvb.PointerTable.Count + 1}",
            //                    ItemLifeTime = ConversationItemLifeTime.AlreadyExisted
            //                };

            //                conversationPointerTextWithYesNo.Lines = GetSingleTextFromFile(conversationCsvb, fileBytes, conversationPointerTextWithYesNo.OffsetValue);
            //                conversationPointerTextWithYesNo.OriginalByteLengthWithZeros = GetTextByteLengthWithZeros(conversationCsvb, fileBytes, conversationPointerTextWithYesNo.OffsetValue);

            //                conversationPointer!.TextBoxes.Add(conversationPointerTextWithYesNo);
            //                break;
            //            case ConversationPointerType.YesNoSelectYesStart:
            //                isInYesSection = true;
            //                break;
            //            case ConversationPointerType.YesNoSelectNoStart:
            //                isInYesSection = false;
            //                break;
            //            case ConversationPointerType.YesNoSelectYesOrNoEnd:
            //                if (isInsideOfYesNoSection && !isInYesSection)
            //                {
            //                    isInsideOfYesNoSection = false;
            //                }

            //                ConversationPointerTextWithYesNo yesNoSectionEnd = (ConversationPointerTextWithYesNo)conversationPointer!.TextBoxes.Last();

            //                if (isInYesSection)
            //                {
            //                    yesNoSectionEnd.YesPointer.Add(conversationPointerYesNo!);
            //                }
            //                else
            //                {
            //                    yesNoSectionEnd.NoPointer.Add(conversationPointerYesNo!);
            //                }

            //                conversationPointerYesNo = null;
            //                break;
            //            case ConversationPointerType.Alert:
            //                extraType = type;
            //                break;
            //            case ConversationPointerType.EmptyTextBox:
            //                NewConversation(pointer, type, 4);
            //                break;
            //            default:
            //                throw new ArgumentOutOfRangeException(nameof(type));
            //        }
            //    }

            //    //some files e.g. C1_012.csvb do not have type 0x0D at the end
            //    if (isInsideOfYesNoSection)
            //    {
            //        ConversationPointerTextWithYesNo yesNoSectionEnd = (ConversationPointerTextWithYesNo)conversationPointer!.TextBoxes.Last();

            //        yesNoSectionEnd.NoPointer.Add(conversationPointerYesNo!);
            //    }

            //    conversationCsvb.PointerTable.Add(conversationPointer!);

            //    return conversationCsvb;

            //    void NewConversation(int pointer, ConversationPointerType type, int offsetValueLength = 8)
            //    {
            //        if (isInsideOfYesNoSection)
            //        {
            //            if (conversationPointerYesNo != null)
            //            {
            //                ConversationPointerTextWithYesNo yesNoSection = (ConversationPointerTextWithYesNo)conversationPointer!.TextBoxes.Last();

            //                if (isInYesSection)
            //                {
            //                    yesNoSection.YesPointer.Add(conversationPointerYesNo);
            //                }
            //                else
            //                {
            //                    yesNoSection.NoPointer.Add(conversationPointerYesNo);
            //                }
            //            }
            //        }
            //        else
            //        {
            //            if (conversationPointer != null)
            //            {
            //                conversationCsvb.PointerTable.Add(conversationPointer);
            //            }
            //        }

            //        if (isInsideOfYesNoSection)
            //        {
            //            byte[] characterOffsetBytesInYesNo = new byte[8];
            //            Array.Copy(fileBytes, conversationCsvb.FileSizeWithUnimportantInfo + pointer, characterOffsetBytesInYesNo, 0, offsetValueLength);
            //            conversationPointerYesNo = new ConversationPointerCharacter
            //            {
            //                Type = type,
            //                OffsetValue = BitConverter.ToInt64(characterOffsetBytesInYesNo)
            //            };

            //            return;
            //        }

            //        byte[] characterOffsetBytes = new byte[8];
            //        Array.Copy(fileBytes, conversationCsvb.FileSizeWithUnimportantInfo + pointer, characterOffsetBytes, 0, offsetValueLength);
            //        conversationPointer = new ConversationPointerCharacter
            //        {
            //            Type = type,
            //            OffsetValue = BitConverter.ToInt64(characterOffsetBytes),
            //            ExtraType = extraType,
            //        };

            //        extraType = null;
            //    }
            //}
            //catch (Exception ex)
            //{
            //    throw;
            //}
        }

        private static async Task<byte[]> GetFileHeader(Stream fileStream)
        {
            fileStream.Seek(0, SeekOrigin.Begin);

            byte[] bytes = new byte[32];

            await fileStream.ReadExactlyAsync(bytes, 0, 32).ConfigureAwait(false);

            return bytes;
        }

        private static async Task<byte[]> GetMapiHeader(Stream fileStream)
        {
            fileStream.Seek(32, SeekOrigin.Begin);

            byte[] bytes = new byte[48];

            await fileStream.ReadExactlyAsync(bytes, 0, 48).ConfigureAwait(false);

            return bytes;
        }

        private static async Task<byte[]> GetConstBytesAfterTextSection(Stream fileStream, int startOffset, int offsetTableOffset)
        {
            fileStream.Seek(startOffset, SeekOrigin.Begin);

            int lengthOfSection = offsetTableOffset - startOffset;

            byte[] bytes = new byte[lengthOfSection];

            await fileStream.ReadExactlyAsync(bytes, 0, lengthOfSection).ConfigureAwait(false);

            return bytes;
        }

        private ObservableCollection<CsvbTextLine> GetSingleTextFromFile(CsvbFile csvbFile, byte[] fileBytes, int offset)
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

        private static int GetTextByteLengthWithZeros(CsvbFile csvbFile, byte[] fileBytes, int offset)
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

        public async Task SaveFile(Stream fileStream, ConversationCsvb conversationCsvb)
        {
            throw new NotImplementedException();

            //try
            //{
            //    CalculateNewValues(conversationCsvb);

            //    int newHeaderSizeToEndOfPointerTable = 0;
            //    int newFullHeaderSize = 0;
            //    int newFileSizeToEndOfText = 0;
            //    int newFileSizeToStartOfOffsetTable = 0;

            //    await ClearFile(fileStream).ConfigureAwait(false);

            //    fileStream.Seek(0, SeekOrigin.Begin);

            //    await fileStream.WriteAsync(conversationCsvb.HeaderBytes).ConfigureAwait(false);
            //    await fileStream.WriteAsync(conversationCsvb.MapiHeaderBytes).ConfigureAwait(false);

            //    await WritePointerTable(fileStream, conversationCsvb).ConfigureAwait(false);

            //    newHeaderSizeToEndOfPointerTable = Convert.ToInt32(fileStream.Length - 8);//minus 8 bytes for the end declartion bytes of the header
            //    newFullHeaderSize = Convert.ToInt32(fileStream.Length);

            //    await WriteTextTable(fileStream, conversationCsvb).ConfigureAwait(false);

            //    newFileSizeToEndOfText = Convert.ToInt32(fileStream.Length);

            //    await fileStream.WriteAsync(conversationCsvb.AfterTextSectionBytes).ConfigureAwait(false);

            //    newFileSizeToStartOfOffsetTable = Convert.ToInt32(fileStream.Length);

            //    await WriteOffsetTable(fileStream, conversationCsvb).ConfigureAwait(false);

            //    await OverrideHeaderInformation(fileStream, newHeaderSizeToEndOfPointerTable, newFullHeaderSize, newFileSizeToEndOfText, newFileSizeToStartOfOffsetTable).ConfigureAwait(false);

            //    await OverridePointerCount(fileStream, conversationCsvb.CountOfPointersInFile);
            //}
            //catch (Exception e)
            //{
            //    throw;
            //}
        }

        private static async Task ClearFile(Stream fileStream)
        {
            fileStream.SetLength(0);
            await fileStream.FlushAsync().ConfigureAwait(false);
        }

        private static async Task WritePointerTable(Stream fileStream, ConversationCsvb conversationCsvb)
        {
            const int currentPointerValue = 4;

            await WriteConversationPointers(fileStream, conversationCsvb.PointerTable, currentPointerValue).ConfigureAwait(false);

            await fileStream.WriteAsync(new byte[] { 2, 0, 0, 0 }).ConfigureAwait(false);
            await fileStream.WriteAsync(new byte[] { 2, 0, 0, 0 }).ConfigureAwait(false);

            static async Task<int> WriteConversationPointers(Stream fileStream, IReadOnlyCollection<ConversationPointerCharacter> conversationPointerCharacters, int currentPointerValue)
            {
                foreach (ConversationPointerCharacter conversationPointerCharacter in conversationPointerCharacters)
                {
                    if (conversationPointerCharacter.ExtraType != null)
                    {
                        await fileStream.WriteAsync(BitConverter.GetBytes(currentPointerValue)).ConfigureAwait(false);
                        await fileStream.WriteAsync(BitConverter.GetBytes((int)conversationPointerCharacter.ExtraType)).ConfigureAwait(false);
                    }

                    if (conversationPointerCharacter.OffsetValue != -1)
                    {
                        await fileStream.WriteAsync(BitConverter.GetBytes(currentPointerValue)).ConfigureAwait(false);
                        await fileStream.WriteAsync(BitConverter.GetBytes((int)conversationPointerCharacter.Type)).ConfigureAwait(false);

                        if (conversationPointerCharacter.Type == ConversationPointerType.EmptyTextBox)
                        {
                            currentPointerValue += 4;
                            continue;
                        }
                        else
                        {
                            currentPointerValue += 8;
                        }
                    }

                    foreach (ConversationPointerText conversationPointerText in conversationPointerCharacter.TextBoxes)
                    {
                        switch (conversationPointerText.PointerItemType)
                        {
                            case ConversationPointerTextType.Normal:
                                await fileStream.WriteAsync(BitConverter.GetBytes(currentPointerValue)).ConfigureAwait(false);
                                await fileStream.WriteAsync(BitConverter.GetBytes((int)conversationPointerText.Type)).ConfigureAwait(false);

                                currentPointerValue += 4;
                                break;
                            case ConversationPointerTextType.YesNo:
                                ConversationPointerTextWithYesNo conversationPointerTextWithYesNo = (ConversationPointerTextWithYesNo)conversationPointerText;

                                await fileStream.WriteAsync(BitConverter.GetBytes(currentPointerValue)).ConfigureAwait(false);
                                await fileStream.WriteAsync(BitConverter.GetBytes((int)conversationPointerTextWithYesNo.Type)).ConfigureAwait(false);

                                currentPointerValue += 4;

                                //indicate start of yes section
                                await fileStream.WriteAsync(BitConverter.GetBytes(currentPointerValue)).ConfigureAwait(false);
                                await fileStream.WriteAsync(BitConverter.GetBytes(0x09)).ConfigureAwait(false);

                                currentPointerValue = await WriteConversationPointers(fileStream, conversationPointerTextWithYesNo.YesPointer, currentPointerValue).ConfigureAwait(false);

                                //indicate end of yes section
                                await fileStream.WriteAsync(BitConverter.GetBytes(currentPointerValue)).ConfigureAwait(false);
                                await fileStream.WriteAsync(BitConverter.GetBytes(0x0D)).ConfigureAwait(false);

                                //indicate start of no section
                                await fileStream.WriteAsync(BitConverter.GetBytes(currentPointerValue)).ConfigureAwait(false);
                                await fileStream.WriteAsync(BitConverter.GetBytes(0x0A)).ConfigureAwait(false);

                                currentPointerValue = await WriteConversationPointers(fileStream, conversationPointerTextWithYesNo.NoPointer, currentPointerValue).ConfigureAwait(false);

                                //indicate end of no section
                                await fileStream.WriteAsync(BitConverter.GetBytes(currentPointerValue)).ConfigureAwait(false);
                                await fileStream.WriteAsync(BitConverter.GetBytes(0x0D)).ConfigureAwait(false);
                                break;
                            case ConversationPointerTextType.TextWithChangingCharImage:
                                await fileStream.WriteAsync(BitConverter.GetBytes(currentPointerValue)).ConfigureAwait(false);
                                await fileStream.WriteAsync(BitConverter.GetBytes((int)conversationPointerText.Type)).ConfigureAwait(false);

                                currentPointerValue += 16; //12 bytes extra offset values
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(conversationPointerText.PointerItemType));
                        }
                    }
                }

                return currentPointerValue;
            }
        }

        private async Task WriteTextTable(Stream fileStream, ConversationCsvb conversationCsvb)
        {
            await fileStream.WriteAsync(new byte[] { 0, 0, 0, 0 }).ConfigureAwait(false);

            await WriteConversationText(fileStream, conversationCsvb.PointerTable).ConfigureAwait(false);
        }

        private async Task WriteConversationText(Stream fileStream, IReadOnlyCollection<ConversationPointerCharacter> conversationPointerCharacters)
        {
            foreach (ConversationPointerCharacter conversationPointerCharacter in conversationPointerCharacters)
            {
                foreach (ConversationPointerText conversationPointerText in conversationPointerCharacter.TextBoxes)
                {
                    switch (conversationPointerText.PointerItemType)
                    {
                        case ConversationPointerTextType.Normal:
                        case ConversationPointerTextType.TextWithChangingCharImage:
                            byte[] newTextBytes = GetTextBytesForFile(conversationPointerText.Lines, conversationPointerText.OriginalByteLengthWithZeros);
                            await fileStream.WriteAsync(newTextBytes).ConfigureAwait(false);
                            break;
                        case ConversationPointerTextType.YesNo:
                            ConversationPointerTextWithYesNo conversationPointerTextWithYesNo = (ConversationPointerTextWithYesNo)conversationPointerText;

                            byte[] newTextBytesYesNo = GetTextBytesForFile(conversationPointerTextWithYesNo.Lines, conversationPointerTextWithYesNo.OriginalByteLengthWithZeros);
                            await fileStream.WriteAsync(newTextBytesYesNo).ConfigureAwait(false);

                            await WriteConversationText(fileStream, conversationPointerTextWithYesNo.YesPointer).ConfigureAwait(false);
                            await WriteConversationText(fileStream, conversationPointerTextWithYesNo.NoPointer).ConfigureAwait(false);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(conversationPointerText.PointerItemType));
                    }
                }
            }
        }

        private byte[] GetTextBytesForFile(IReadOnlyCollection<CsvbTextLine> conversationTextLines, int originalByteLengthWithZeros)
        {
            List<byte> textBytes = new List<byte>();
            foreach (CsvbTextLine item in conversationTextLines)
            {
                if (textBytes.Any())
                {
                    textBytes.Add(0x0A); //add new line
                }

                textBytes.AddRange(_shiftJISEncoding.GetBytes(string.IsNullOrEmpty(item.MutableText) ? " " : item.MutableText));
            }

            return AddTrailingZeros(textBytes.ToArray(), textBytes.Count - originalByteLengthWithZeros);
        }

        private static async Task WriteOffsetTable(Stream fileStream, ConversationCsvb conversationCsvb)
        {
            await fileStream.WriteAsync(new byte[] { 0x0A, 0, 0, 0 }).ConfigureAwait(false);

            await WriteConversationOffsetToOffsetTable(fileStream, conversationCsvb.PointerTable).ConfigureAwait(false);

            static async Task WriteConversationOffsetToOffsetTable(Stream fileStream, IReadOnlyCollection<ConversationPointerCharacter> conversationPointerCharacters)
            {
                foreach (ConversationPointerCharacter conversationPointerCharacter in conversationPointerCharacters)
                {
                    if (conversationPointerCharacter.OffsetValue != -1)
                    {
                        if (conversationPointerCharacter.Type == ConversationPointerType.EmptyTextBox)
                        {
                            await fileStream.WriteAsync(BitConverter.GetBytes(Convert.ToInt32(conversationPointerCharacter.OffsetValue))).ConfigureAwait(false);
                            continue;
                        }

                        await fileStream.WriteAsync(BitConverter.GetBytes(conversationPointerCharacter.OffsetValue)).ConfigureAwait(false);
                    }

                    foreach (ConversationPointerText conversationPointerText in conversationPointerCharacter.TextBoxes)
                    {
                        switch (conversationPointerText.PointerItemType)
                        {
                            case ConversationPointerTextType.Normal:
                                await fileStream.WriteAsync(BitConverter.GetBytes(conversationPointerText.OffsetValue)).ConfigureAwait(false);
                                break;
                            case ConversationPointerTextType.YesNo:
                                ConversationPointerTextWithYesNo conversationPointerTextWithYesNo = (ConversationPointerTextWithYesNo)conversationPointerText;

                                await fileStream.WriteAsync(BitConverter.GetBytes(conversationPointerTextWithYesNo.OffsetValue)).ConfigureAwait(false);

                                await WriteConversationOffsetToOffsetTable(fileStream, conversationPointerTextWithYesNo.YesPointer).ConfigureAwait(false);
                                await WriteConversationOffsetToOffsetTable(fileStream, conversationPointerTextWithYesNo.NoPointer).ConfigureAwait(false);
                                break;
                            case ConversationPointerTextType.TextWithChangingCharImage:
                                await fileStream.WriteAsync(BitConverter.GetBytes(conversationPointerText.OffsetValue)).ConfigureAwait(false);
                                await fileStream.WriteAsync(((ConversationPointerTextWithChangingCharImage)conversationPointerText).ChangingCharImageOffsetBytes.ToArray()).ConfigureAwait(false);
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(conversationPointerText.PointerItemType));
                        }
                    }
                }
            }
        }

        private byte[] AddTrailingZeros(byte[] newTextBytes, int lengthDifference)
        {
            List<byte> newTextBytesList = new List<byte>(newTextBytes);

            if (lengthDifference >= 0)
            {
                //check if 0x00s are missing for 4 byte entry structure
                int moduloResult = newTextBytesList.Count % 4;

                for (int i = 0; i < (4 - moduloResult); i++)
                {
                    newTextBytesList.Add(0x00);
                }

                return newTextBytesList.ToArray();
            }

            //if length < 0 replace missing chars with 0x00
            for (int i = 0; i < lengthDifference * -1; i++)
            {
                newTextBytesList.Add(0x00);
            }

            return newTextBytesList.ToArray();
        }

        private static async Task OverrideHeaderInformation(Stream fileStream, int newHeaderSizeToEndOfPointerTable, int newFullHeaderSize, int newFileSizeToEndOfText, int newFileSizeToStartOfOffsetTable)
        {
            fileStream.Seek(12, SeekOrigin.Begin);
            await fileStream.WriteAsync(BitConverter.GetBytes(newHeaderSizeToEndOfPointerTable)).ConfigureAwait(false);

            fileStream.Seek(16, SeekOrigin.Begin);
            await fileStream.WriteAsync(BitConverter.GetBytes(newFullHeaderSize)).ConfigureAwait(false);

            fileStream.Seek(20, SeekOrigin.Begin);
            await fileStream.WriteAsync(BitConverter.GetBytes(newFileSizeToEndOfText)).ConfigureAwait(false);

            fileStream.Seek(24, SeekOrigin.Begin);
            await fileStream.WriteAsync(BitConverter.GetBytes(newFileSizeToStartOfOffsetTable)).ConfigureAwait(false);
        }

        private static async Task OverridePointerCount(Stream fileStream, int pointerCount)
        {
            fileStream.Seek(0x34, SeekOrigin.Begin);
            await fileStream.WriteAsync(BitConverter.GetBytes(pointerCount)).ConfigureAwait(false);
        }

        private void CalculateNewValues(ConversationCsvb conversationCsvb)
        {
            int currentOffsetValue = 4;

            RecursiveCalculate(conversationCsvb.PointerTable);

            void RecursiveCalculate(IList<ConversationPointerCharacter> conversationPointerCharacters)
            {
                foreach (ConversationPointerCharacter conversationPointerCharacter in conversationPointerCharacters)
                {
                    foreach (ConversationPointerText conversationPointerText in conversationPointerCharacter.TextBoxes)
                    {
                        int additionalBytesCount = GetTextBytesForFile(conversationPointerText.Lines, conversationPointerText.OriginalByteLengthWithZeros).Length;

                        conversationPointerText.OffsetValue = currentOffsetValue;

                        currentOffsetValue += additionalBytesCount;

                        if (conversationPointerText.ItemLifeTime == ConversationItemLifeTime.New)
                        {
                            conversationCsvb.CountOfPointersInFile++;
                        }

                        if (conversationPointerText.PointerItemType == ConversationPointerTextType.YesNo)
                        {
                            ConversationPointerTextWithYesNo conversationPointerTextWithYesNo = (ConversationPointerTextWithYesNo)conversationPointerText;

                            RecursiveCalculate(conversationPointerTextWithYesNo.YesPointer);
                            RecursiveCalculate(conversationPointerTextWithYesNo.NoPointer);
                        }
                    }
                }
            }
        }
    }
}
