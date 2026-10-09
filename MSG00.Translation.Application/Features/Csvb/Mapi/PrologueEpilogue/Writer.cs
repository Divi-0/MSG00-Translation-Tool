using MSG00.Translation.Application.Features.Csvb.Write;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue;

public interface IWriter
{
    Task WriteFileAsync(Stream stream, PrologueCsvb proEpilogueCsvb);
}

public class Writer : CsvbWriter, IWriter
{
    public async Task WriteFileAsync(Stream stream, PrologueCsvb proEpilogueCsvb)
    {
        try
        {
            await ClearFile(stream).ConfigureAwait(false);
            await CreateCsvbHeaderAsync(stream).ConfigureAwait(false);
            await WritePreviousMapiHeader(stream, proEpilogueCsvb).ConfigureAwait(false);
            
            CalculateOffsets(proEpilogueCsvb.ProEpiloguePointers);
            
            await WritePointerTable(stream, proEpilogueCsvb).ConfigureAwait(false);
            var newOffsetToEndOfPointerTable = Convert.ToInt32(stream.Length);
            
            await WriteExtraBytesAtEndOfFullHeaderAsync(stream).ConfigureAwait(false);
            var newOffsetToEndOfFullHeader = Convert.ToInt32(stream.Length);
            
            await WriteTextTable(stream, proEpilogueCsvb).ConfigureAwait(false);
            var newOffsetToEndOfTextTable = Convert.ToInt32(stream.Length);

            await stream.WriteAsync(proEpilogueCsvb.AfterTextSectionBytes).ConfigureAwait(false);
            var newOffsetToEndOfExtraTable = Convert.ToInt32(stream.Length);

            await WriteOffsetTable(stream, proEpilogueCsvb);
            
            await CsvbWriter.OverrideCsvbHeaderAsync(stream, newOffsetToEndOfPointerTable, newOffsetToEndOfFullHeader, newOffsetToEndOfTextTable, newOffsetToEndOfExtraTable).ConfigureAwait(false);
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

    private static async Task WritePreviousMapiHeader(Stream stream, PrologueCsvb proEpilogueCsvb)
    {
        await stream.WriteAsync(proEpilogueCsvb.MapiHeader.FullHeaderBytes).ConfigureAwait(false);
    }

    private static async Task WriteExtraBytesAtEndOfFullHeaderAsync(Stream stream)
    {
        await stream.WriteAsync(new byte[] { 0x02, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
        await stream.WriteAsync(new byte[] { 0x02, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
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
                case ProloguePointerType.ShowImage:
                case ProloguePointerType.ScriptStart:
                case ProloguePointerType.Unknown0C:
                    currentPointer += 4;
                    break;
                default:
                    currentPointer += 8;
                    break;
            }
        }
    }

    private async Task WriteTextTable(Stream stream, PrologueCsvb proEpilogueCsvb)
    {
        await stream.WriteAsync(new byte[] { 0, 0, 0, 0 }).ConfigureAwait(false);

        Dictionary<string, bool> writtenImageNames = new Dictionary<string, bool>();

        await WriteTextToTextTable(stream, proEpilogueCsvb.ProEpiloguePointers, writtenImageNames).ConfigureAwait(false);
    }

    private async Task WriteTextToTextTable(Stream stream, List<ProloguePointer> proEpiloguePointers, Dictionary<string, bool> writtenImageNames)
    {
        foreach (ProloguePointer proEpiloguePointer in proEpiloguePointers)
        {
            switch (proEpiloguePointer)
            {
                case ProloguePointerImageReference imageReference:
                    foreach (string imageName in imageReference.ImageNames)
                    {
                        if (writtenImageNames.ContainsKey(imageName))
                        {
                            continue;
                        }

                        byte[] newTextBytes = _shiftJISEncoding.GetBytes(imageName).AddTrailingCsvbFileZeroes();

                        await stream.WriteAsync(newTextBytes).ConfigureAwait(false);

                        writtenImageNames.Add(imageName, true);
                    }
                    break;
                case ProloguePointerText proEpiloguePointerText:
                    // List<byte> fullTextBytes = new List<byte>();
                    // foreach (CsvbTextLine csvbTextLine in proEpiloguePointerText.TextLines)
                    // {
                    //     if (fullTextBytes.Any())
                    //     {
                    //         fullTextBytes.Add(0x0A);
                    //     }
                    //
                    //     fullTextBytes.AddRange(_shiftJISEncoding.GetBytes(csvbTextLine.MutableText));
                    // }

                    //byte[] newTextbytes = fullTextBytes.ToArray().AddTrailingCsvbFileZeroes();
                    byte[] newTextbytes = _shiftJISEncoding.GetBytes(proEpiloguePointerText.TextLines).AddTrailingCsvbFileZeroes();

                    await stream.WriteAsync(newTextbytes).ConfigureAwait(false);
                    break;
            }

            await WriteTextToTextTable(stream, proEpiloguePointer.SubPointer, writtenImageNames).ConfigureAwait(false);
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
            if (!proEpiloguePointer.HasSlot)
            {
                continue;
            }

            switch (proEpiloguePointer.Type)
            {
                case ProloguePointerType.ScriptStart:
                case ProloguePointerType.ShowImage:
                case ProloguePointerType.Unknown0C:
                    await stream.WriteAsync(BitConverter.GetBytes(Convert.ToInt32(proEpiloguePointer.OffsetValue))).ConfigureAwait(false);
                    break;
                default:
                    await stream.WriteAsync(BitConverter.GetBytes(proEpiloguePointer.OffsetValue)).ConfigureAwait(false);
                    break;
            }

            //no subpointers
        }
    }

    private int CalculateOffsets(List<ProloguePointer> proEpiloguePointers, Dictionary<string, int>? writtenImageNames = null, int currentOffsetValue = 4)
    {
        if (writtenImageNames == null)
        {
            writtenImageNames = new Dictionary<string, int>();
        }

        foreach (ProloguePointer proEpiloguePointer in proEpiloguePointers)
        {
            switch (proEpiloguePointer)
            {
                case { HasSlot: false }:
                    break;
                case ProloguePointerImageReference imageReference:
                    List<byte> offsetBytes = new List<byte>();
                    foreach (string imageName in imageReference.ImageNames)
                    {
                        if (writtenImageNames.TryGetValue(imageName, out int offsetValue))
                        {
                            offsetBytes.AddRange(BitConverter.GetBytes(offsetValue));
                            continue;
                        }

                        offsetBytes.AddRange(BitConverter.GetBytes(currentOffsetValue));

                        writtenImageNames.Add(imageName, currentOffsetValue);

                        currentOffsetValue += _shiftJISEncoding.GetBytes(imageName).AddTrailingCsvbFileZeroes().Length;
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

                    imageReference.OffsetValue = BitConverter.ToInt64(offsetBytes.ToArray());
                    break;
                case ProloguePointerText proEpiloguePointerText:
                    // List<byte> fullTextBytes = new List<byte>();
                    // foreach (CsvbTextLine csvbTextLine in proEpiloguePointerText.TextLines)
                    // {
                    //     if (fullTextBytes.Any())
                    //     {
                    //         fullTextBytes.Add(0x0A);
                    //     }
                    //
                    //     fullTextBytes.AddRange(_shiftJISEncoding.GetBytes(csvbTextLine.MutableText));
                    // }

                    List<byte> textOffsetBytes = new List<byte>();
                    textOffsetBytes.AddRange(BitConverter.GetBytes(currentOffsetValue));

                    byte[] additionalBytes = new byte[4];
                    Array.Copy(BitConverter.GetBytes(proEpiloguePointerText.OffsetValue), 4, additionalBytes, 0, 4);
                    textOffsetBytes.AddRange(additionalBytes);


                    proEpiloguePointerText.OffsetValue = BitConverter.ToInt64(textOffsetBytes.ToArray());
                    //currentOffsetValue += fullTextBytes.ToArray().AddTrailingCsvbFileZeroes().Length;
                    currentOffsetValue += _shiftJISEncoding.GetBytes(proEpiloguePointerText.TextLines).AddTrailingCsvbFileZeroes().Length;
                    break;
            }

            currentOffsetValue = CalculateOffsets(proEpiloguePointer.SubPointer, writtenImageNames, currentOffsetValue);
        }

        return currentOffsetValue;
    }
}