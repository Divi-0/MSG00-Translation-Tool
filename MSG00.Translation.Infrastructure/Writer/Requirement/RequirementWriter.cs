using MSG00.Translation.Infrastructure.Domain.Requirement;
using MSG00.Translation.Infrastructure.Extensions;

namespace MSG00.Translation.Infrastructure.Writer.Requirement
{
    internal class RequirementWriter : BlockWriter, IRequirementWriter
    {
        public async Task WriteFile(Stream stream, RequirementCsvb etcCsvb)
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
            await OverrideBlockHeader(stream, etcCsvb.Pointers.Count).ConfigureAwait(false);
        }

        protected async override Task CreateHeader(Stream stream)
        {
            await base.CreateHeader(stream);

            await stream.WriteAsync(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x48, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
        }

        private async Task ClearFile(Stream stream)
        {
            stream.SetLength(0);
            await stream.FlushAsync().ConfigureAwait(false);
        }

        private async Task WritePointerTable(Stream stream, RequirementCsvb etcCsvb)
        {
            int currentPointer = 4;
            foreach (RequirementPointer etcPointer in etcCsvb.Pointers)
            {
                await stream.WriteAsync(BitConverter.GetBytes(currentPointer)).ConfigureAwait(false);

                currentPointer += _shiftJISEncoding.GetBytes(etcPointer.Text).AddTrailingCsvbFileZeroes().Length;
            }
        }

        private async Task WriteExtraPointerTableBytes(Stream stream)
        {
            await stream.WriteAsync(BitConverter.GetBytes(7)).ConfigureAwait(false);
        }

        private async Task WriteTextTable(Stream stream, RequirementCsvb etcCsvb)
        {
            await stream.WriteAsync(BitConverter.GetBytes(0)).ConfigureAwait(false);

            foreach (RequirementPointer etcPointer in etcCsvb.Pointers)
            {
                await stream.WriteAsync(_shiftJISEncoding.GetBytes(etcPointer.Text).AddTrailingCsvbFileZeroes()).ConfigureAwait(false);
            }
        }
    }
}
