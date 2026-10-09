
namespace MSG00.Translation.Application.Features.Csvb.CutIn;

public interface IWriter
{
    Task WriteFileAsync(Stream stream, CutInCsvb cutInCsvb);
}

/// <summary>
/// Writes changed lines with <see cref="SlotTextWriter"/> (in place or appended, existing data never moves).
/// </summary>
public class Writer : IWriter
{
    public async Task WriteFileAsync(Stream stream, CutInCsvb cutInCsvb)
    {
        var texts = cutInCsvb.Lines
            .Select(x => new SlotText(x.SlotOffset, Reader.TextOffsetInSlot, x.TextOffset, x.Original, x.Text))
            .ToList();

        var bytes = SlotTextWriter.Write(cutInCsvb.Bytes, cutInCsvb.CsvbHeader.OffsetToEndOfFullHeader, texts, new HashSet<int>());

        stream.SetLength(0);
        await stream.WriteAsync(bytes).ConfigureAwait(false);
    }
}