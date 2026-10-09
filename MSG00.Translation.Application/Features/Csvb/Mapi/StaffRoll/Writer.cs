
namespace MSG00.Translation.Application.Features.Csvb.Mapi.StaffRoll;

public interface IWriter
{
    Task WriteFileAsync(Stream stream, StaffRollTextCsvb staffRoll);
}

/// <summary>
/// Writes changed texts with <see cref="SlotTextWriter"/> (in place or appended, existing data never moves).
/// </summary>
public class Writer : IWriter
{
    public async Task WriteFileAsync(Stream stream, StaffRollTextCsvb staffRoll)
    {
        var texts = staffRoll.Texts
            .Select(x => new SlotText(x.SlotOffset, Reader.TextOffsetInSlot, x.TextOffset, x.Original, x.Text))
            .ToList();

        var bytes = SlotTextWriter.Write(staffRoll.Bytes, staffRoll.CsvbHeader.OffsetToEndOfFullHeader, texts, new HashSet<int>());

        stream.SetLength(0);
        await stream.WriteAsync(bytes).ConfigureAwait(false);
    }
}