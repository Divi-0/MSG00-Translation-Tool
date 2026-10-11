using MSG00.Translation.Application.Features.Csvb.Mapi;
using MSG00.Translation.Application.Features.Csvb.Read;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue;

public interface IReader
{
    Task<PrologueCsvb> ReadFileAsync(MapiCsvb mapiCsvb, Stream stream);
}

internal sealed class Reader : CsvbMapiReader, IReader
{
    public async Task<PrologueCsvb> ReadFileAsync(MapiCsvb mapiCsvb, Stream stream)
    {
        try
        {
            byte[] fileBytes = new byte[stream.Length];

            stream.Position = 0;
            _ = await stream.ReadAsync(fileBytes).ConfigureAwait(false);
                
            var prologueEpilogueCsvb = new PrologueCsvb
            {
                CsvbHeader = mapiCsvb.CsvbHeader,
                MapiHeader = mapiCsvb.MapiHeader,
                AfterTextSectionBytes = await CsvbMapiReader.GetConstBytesAfterTextTable(
                        stream, 
                        BitConverter.ToInt32(mapiCsvb.CsvbHeader.OffsetToEndOfTextTable), 
                        mapiCsvb.CsvbHeader.OffsetToEndOfExtraTable)
                    .ConfigureAwait(false),
            };

            var lastPointer = -1;
            for (var i = 0x50; i < prologueEpilogueCsvb.CsvbHeader.OffsetToEndOfPointerTable; i += 8)
            {
                byte[] pointerBytes = new byte[4];
                Array.Copy(fileBytes, i, pointerBytes, 0, 4);
                int pointer = BitConverter.ToInt32(pointerBytes);

                byte[] typeBytes = new byte[4];
                Array.Copy(fileBytes, i + 4, typeBytes, 0, 4);
                ProloguePointerType type = (ProloguePointerType)BitConverter.ToInt32(typeBytes);

                var isSamePointer = false;
                if (pointer == lastPointer)
                {
                    isSamePointer = true;
                }
                else
                {
                    isSamePointer = false;
                }

                if (fileBytes.Length < prologueEpilogueCsvb.CsvbHeader.OffsetToEndOfExtraTable + pointer + 4)
                {
                    // the file ends before this slot (last group of the Epilogues): keep the entry, write no slot
                    AddPointer(new ProloguePointer { Type = type, OffsetValue = -1, HasSlot = false }, isSamePointer);
                    lastPointer = pointer;
                    continue;
                }

                switch (type)
                {
                    case ProloguePointerType.SceneChange:
                    case ProloguePointerType.SceneChangeEnd:
                        // in the Epilogues these slots name two images ("Epi_02", "Epilogue_02") like LoadImage
                        if (IsImageNameSlot(fileBytes, prologueEpilogueCsvb, pointer))
                        {
                            goto case ProloguePointerType.LoadImage;
                        }

                        goto case ProloguePointerType.TransitionBefore;
                    case ProloguePointerType.TransitionBefore:
                    case ProloguePointerType.TransitionAfter:
                    case ProloguePointerType.ClearText:
                    case ProloguePointerType.Unknown0B:
                        AddPointer(new ProloguePointer
                        {
                            Type = type,
                            OffsetValue = BitConverter.ToInt64(GetOffsetBytes(fileBytes, prologueEpilogueCsvb, pointer, 8)),
                        }, isSamePointer);
                        break;
                    case ProloguePointerType.ScriptStart:
                    case ProloguePointerType.ShowImage:
                    case ProloguePointerType.Unknown0C:
                        AddPointer(new ProloguePointer
                        {
                            Type = type,
                            OffsetValue = BitConverter.ToInt32(GetOffsetBytes(fileBytes, prologueEpilogueCsvb, pointer, 4)),
                        }, isSamePointer);
                        break;
                    case ProloguePointerType.Text:
                        byte[] textOffsetBytes = GetOffsetBytes(fileBytes, prologueEpilogueCsvb, pointer, 8);

                        ProloguePointerText prologuePointerText = new ProloguePointerText
                        {
                            Type = type,
                            OffsetValue = BitConverter.ToInt64(textOffsetBytes)
                        };

                        prologuePointerText.TextLines = GetSingleTextFromFile(prologueEpilogueCsvb, fileBytes, BitConverter.ToInt32(new byte[] { textOffsetBytes[0], textOffsetBytes[1], textOffsetBytes[2], textOffsetBytes[3] }));

                        AddPointer(prologuePointerText, isSamePointer);
                        break;
                    case ProloguePointerType.LoadImage:
                    case ProloguePointerType.SwitchImage:
                    case ProloguePointerType.Unknown0A:
                        ProloguePointerImageReference imageReference = new ProloguePointerImageReference
                        {
                            Type = type,
                            OffsetValue = BitConverter.ToInt64(GetOffsetBytes(fileBytes, prologueEpilogueCsvb, pointer, 8))
                        };

                        if (imageReference.OffsetValue == -1)
                        {
                            AddPointer(imageReference, isSamePointer);
                            break;
                        }

                        byte[] firstImageNameOffsetBytes = new byte[4];
                        Array.Copy(fileBytes, prologueEpilogueCsvb.CsvbHeader.OffsetToEndOfExtraTable + pointer, firstImageNameOffsetBytes, 0, 4);
                        int firstImageNameOffset = BitConverter.ToInt32(firstImageNameOffsetBytes);

                        byte[] secondImageNameOffsetBytes = new byte[4];
                        Array.Copy(fileBytes, prologueEpilogueCsvb.CsvbHeader.OffsetToEndOfExtraTable + pointer + 4, secondImageNameOffsetBytes, 0, 4);
                        int secondImageNameOffset = BitConverter.ToInt32(secondImageNameOffsetBytes);

                        //imageReference.ImageNames.Add(GetSingleTextFromFile(mapiCsvb, fileBytes, firstImageNameOffset)[0].Text);
                        imageReference.ImageNames.Add(GetSingleTextFromFile(prologueEpilogueCsvb, fileBytes, firstImageNameOffset));

                        if (firstImageNameOffset != secondImageNameOffset)
                        {
                            //imageReference.ImageNames.Add(GetSingleTextFromFile(mapiCsvb, fileBytes, secondImageNameOffset)[0].Text);
                            imageReference.ImageNames.Add(GetSingleTextFromFile(prologueEpilogueCsvb, fileBytes, secondImageNameOffset));
                        }

                        AddPointer(imageReference, isSamePointer);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type));
                }

                lastPointer = pointer;
            }

            return prologueEpilogueCsvb;

            void AddPointer(ProloguePointer proEpiloguePointer, bool isSamePointer)
            {
                if (isSamePointer)
                {
                    prologueEpilogueCsvb.ProEpiloguePointers.Last().SubPointer.Add(proEpiloguePointer);
                    return;
                }

                prologueEpilogueCsvb.ProEpiloguePointers.Add(proEpiloguePointer);
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>both halves of the 8-byte slot point at the start of a string in the text table</summary>
    private static bool IsImageNameSlot(byte[] fileBytes, PrologueCsvb proEpilogueCsvb, int pointer)
    {
        var slot = proEpilogueCsvb.CsvbHeader.OffsetToEndOfExtraTable + pointer;
        if (fileBytes.Length < slot + 8)
        {
            return false;
        }

        var textTableStart = proEpilogueCsvb.CsvbHeader.OffsetToEndOfFullHeader;
        var textTableLength = BitConverter.ToInt32(proEpilogueCsvb.CsvbHeader.OffsetToEndOfTextTable) - textTableStart;
        return IsStringStart(BitConverter.ToInt32(fileBytes, slot)) && IsStringStart(BitConverter.ToInt32(fileBytes, slot + 4));

        bool IsStringStart(int offset) =>
            offset >= 4 && offset < textTableLength && fileBytes[textTableStart + offset] != 0 && fileBytes[textTableStart + offset - 1] == 0;
    }

    private static byte[] GetOffsetBytes(byte[] fileBytes, PrologueCsvb proEpilogueCsvb, int pointer, int offsetLength)
    {
        if (fileBytes.Length < proEpilogueCsvb.CsvbHeader.OffsetToEndOfExtraTable + pointer + offsetLength)
        {
            return BitConverter.GetBytes(offsetLength == 8 ? (long)-1 : -1);
        }

        byte[] offsetBytes = new byte[offsetLength];
        Array.Copy(fileBytes, proEpilogueCsvb.CsvbHeader.OffsetToEndOfExtraTable + pointer, offsetBytes, 0, offsetLength);
        return offsetBytes;
    }
}