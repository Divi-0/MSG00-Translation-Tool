namespace MSG00.Translation.Infrastructure.Writer
{
    internal abstract class BlockWriter : CsvbWriter
    {
        protected async override Task CreateHeader(Stream stream)
        {
            await base.CreateHeader(stream).ConfigureAwait(false);

            await stream.WriteAsync(new byte[] { 0x42, 0x4C, 0x4F, 0x43, 0x4B, 0x31, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
        }

        protected async Task OverrideBlockHeader(Stream stream, int pointerCount)
        {
            stream.Seek(0x34, SeekOrigin.Begin);
            await stream.WriteAsync(BitConverter.GetBytes(pointerCount)).ConfigureAwait(false);
        }
    }
}
