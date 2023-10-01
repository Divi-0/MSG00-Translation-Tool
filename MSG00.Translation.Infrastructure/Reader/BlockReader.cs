using System.Text;

namespace MSG00.Translation.Infrastructure.Reader
{
    internal class BlockReader
    {
        private readonly Encoding _shiftJISEncoding;

        public BlockReader()
        {
            _shiftJISEncoding = Encoding.GetEncoding("Shift-JIS");
        }

        protected async Task<string> GetTextInFile(Stream stream, long offset)
        {
            long currentStreamOffset = stream.Position;

            stream.Seek(offset, SeekOrigin.Begin);

            List<byte> textBytes = new List<byte>();
            while (true)
            {
                byte[] buffer = new byte[1];
                await stream.ReadExactlyAsync(buffer, 0, 1).ConfigureAwait(false);

                if (buffer[0] == 0)
                {
                    break;
                }

                textBytes.Add(buffer[0]);
            }

            stream.Seek(currentStreamOffset, SeekOrigin.Begin);

            return _shiftJISEncoding.GetString(textBytes.ToArray());
        }
    }
}
