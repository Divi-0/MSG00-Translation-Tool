using MSG00.Translation.Infrastructure.Domain.Etc;
using MSG00.Translation.Infrastructure.Extensions;

namespace MSG00.Translation.Infrastructure.Writer.Etc
{
    internal class EtcFgHcmHgWriter : BlockWriter, IEtcFgHcmHgWriter
    {
        public async Task WriteFile(Stream stream, EtcFgHcmHgCsvb etcCsvb)
        {
            await ClearFile(stream).ConfigureAwait(false);

            await CreateHeader(stream).ConfigureAwait(false);
            await WritePointerTable(stream, etcCsvb).ConfigureAwait(false);

            int fileSizePointerTable = Convert.ToInt32(stream.Length);

            await WriteExtraPointerTableBytes(stream).ConfigureAwait(false);

            int fileSizeFullHeader = Convert.ToInt32(stream.Length);

            await WriteTextTable(stream, etcCsvb).ConfigureAwait(false);

            int fileSizeTextTable = Convert.ToInt32(stream.Length);

            await OverrideCsvbHeader(stream, fileSizePointerTable, fileSizeFullHeader, fileSizeTextTable, fileSizeTextTable).ConfigureAwait(false);
            await OverrideBlockHeader(stream, etcCsvb.EtcPointers.Count).ConfigureAwait(false);
        }

        protected async override Task CreateHeader(Stream stream)
        {
            await base.CreateHeader(stream);

            await stream.WriteAsync(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x14, 0x00, 0x00, 0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
        }

        private async Task ClearFile(Stream stream)
        {
            stream.SetLength(0);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        private async Task WritePointerTable(Stream stream, EtcFgHcmHgCsvb etcCsvb)
        {
            int currentPointer = 4;
            foreach (EtcFgHcmHgPointer etcPointer in etcCsvb.EtcPointers)
            {
                await stream.WriteAsync(BitConverter.GetBytes(0)).ConfigureAwait(false);

                await stream.WriteAsync(BitConverter.GetBytes(currentPointer)).ConfigureAwait(false);

                currentPointer += _shiftJISEncoding.GetBytes(etcPointer.Text).AddTrailingCsvbFileZeroes().Length;

                await stream.WriteAsync(BitConverter.GetBytes(currentPointer)).ConfigureAwait(false);
                await stream.WriteAsync(BitConverter.GetBytes(currentPointer)).ConfigureAwait(false);
                await stream.WriteAsync(BitConverter.GetBytes(currentPointer)).ConfigureAwait(false);

                currentPointer += _shiftJISEncoding.GetBytes(etcPointer.GameObjectReference).AddTrailingCsvbFileZeroes().Length;
            }
        }

        private async Task WriteExtraPointerTableBytes(Stream stream)
        {
            await stream.WriteAsync(BitConverter.GetBytes(7)).ConfigureAwait(false);
            await stream.WriteAsync(BitConverter.GetBytes(7)).ConfigureAwait(false);
            await stream.WriteAsync(BitConverter.GetBytes(7)).ConfigureAwait(false);
            await stream.WriteAsync(BitConverter.GetBytes(7)).ConfigureAwait(false);
            await stream.WriteAsync(BitConverter.GetBytes(7)).ConfigureAwait(false);
        }

        private async Task WriteTextTable(Stream stream, EtcFgHcmHgCsvb etcCsvb)
        {
            await stream.WriteAsync(BitConverter.GetBytes(0)).ConfigureAwait(false);

            foreach (EtcFgHcmHgPointer etcPointer in etcCsvb.EtcPointers)
            {
                await stream.WriteAsync(_shiftJISEncoding.GetBytes(etcPointer.Text).AddTrailingCsvbFileZeroes()).ConfigureAwait(false);
                await stream.WriteAsync(_shiftJISEncoding.GetBytes(etcPointer.GameObjectReference).AddTrailingCsvbFileZeroes()).ConfigureAwait(false);
            }
        }
    }
}
