using System.Text;

namespace MSG00.Translation.Application.Features.Csvb.Write;

public abstract class CsvbWriter
{
    protected readonly Encoding _shiftJISEncoding;

    public CsvbWriter()
    {
        _shiftJISEncoding = Encoding.GetEncoding("Shift-JIS");
    }

    protected virtual async Task CreateCsvbHeaderAsync(Stream stream)
    {
        await stream.WriteAsync(new byte[] { 0x43, 0x53, 0x56, 0x42, 0x76, 0x34, 0x2E, 0x33, 0x01, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
        await stream.WriteAsync(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
        await stream.WriteAsync(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
        await stream.WriteAsync(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
        await stream.WriteAsync(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
        await stream.WriteAsync(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
    }

    protected static async Task OverrideCsvbHeaderAsync(Stream stream, int offsetToEndOfPointerTable, int offsetToEndOfFullHeader, int offsetToEndOfTextTable, int offsetToEndOfExtraTable)
    {
        stream.Seek(0x0C, SeekOrigin.Begin);
        await stream.WriteAsync(BitConverter.GetBytes(offsetToEndOfPointerTable)).ConfigureAwait(false);

        stream.Seek(0x10, SeekOrigin.Begin);
        await stream.WriteAsync(BitConverter.GetBytes(offsetToEndOfFullHeader)).ConfigureAwait(false);

        stream.Seek(0x14, SeekOrigin.Begin);
        await stream.WriteAsync(BitConverter.GetBytes(offsetToEndOfTextTable)).ConfigureAwait(false);

        stream.Seek(0x18, SeekOrigin.Begin);
        await stream.WriteAsync(BitConverter.GetBytes(offsetToEndOfExtraTable)).ConfigureAwait(false);
    }

    protected static byte[] AddTrailingZeros(byte[] newTextBytes)
    {
        List<byte> newTextBytesList = new List<byte>(newTextBytes);

        //check if 0x00s are missing for 4 byte entry structure
        int moduloResult = newTextBytesList.Count % 4;

        for (int i = 0; i < (4 - moduloResult); i++)
        {
            newTextBytesList.Add(0x00);
        }

        return newTextBytesList.ToArray();
    }
}