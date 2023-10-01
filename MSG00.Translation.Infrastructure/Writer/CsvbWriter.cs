using System.Text;

namespace MSG00.Translation.Infrastructure.Writer
{
    internal abstract class CsvbWriter
    {
        protected readonly Encoding _shiftJISEncoding;

        public CsvbWriter()
        {
            _shiftJISEncoding = Encoding.GetEncoding("Shift-JIS");
        }

        protected virtual async Task CreateHeader(Stream stream)
        {
            await stream.WriteAsync(new byte[] { 0x43, 0x53, 0x56, 0x42, 0x76, 0x34, 0x2E, 0x33, 0x01, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
            await stream.WriteAsync(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
            await stream.WriteAsync(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
            await stream.WriteAsync(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
            await stream.WriteAsync(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
            await stream.WriteAsync(new byte[] { 0x00, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
        }

        protected async Task OverrideCsvbHeader(Stream stream, int fileSizePointerTable, int fileSizeFullHeader, int fileSizeTextTable, int fileSizeExtraTable)
        {
            stream.Seek(0x0C, SeekOrigin.Begin);
            await stream.WriteAsync(BitConverter.GetBytes(fileSizePointerTable)).ConfigureAwait(false);

            stream.Seek(0x10, SeekOrigin.Begin);
            await stream.WriteAsync(BitConverter.GetBytes(fileSizeFullHeader)).ConfigureAwait(false);

            stream.Seek(0x14, SeekOrigin.Begin);
            await stream.WriteAsync(BitConverter.GetBytes(fileSizeTextTable)).ConfigureAwait(false);

            stream.Seek(0x18, SeekOrigin.Begin);
            await stream.WriteAsync(BitConverter.GetBytes(fileSizeExtraTable)).ConfigureAwait(false);
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
}
