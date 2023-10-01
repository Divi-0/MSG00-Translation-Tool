using MSG00.Translation.Infrastructure.Domain.Etc;
using MSG00.Translation.Infrastructure.Domain.Interfaces;
using MSG00.Translation.Infrastructure.Reader.EtcFgHcmHg;
using MSG00.Translation.Infrastructure.Writer.Etc;

namespace MSG00.Translation.Infrastructure.Services
{
    internal sealed class EtcFgHcmHgService : IEtcFgHcmHgService
    {
        private readonly IEtcFgHcmHgReader _etcReader;
        private readonly IEtcFgHcmHgWriter _etcWriter;

        public EtcFgHcmHgService(IEtcFgHcmHgReader etcReader, IEtcFgHcmHgWriter etcWriter)
        {
            _etcReader = etcReader;
            _etcWriter = etcWriter;
        }

        public async Task<EtcFgHcmHgCsvb> GetEtcCsvbAsync(Stream stream)
        {
            return await _etcReader.ReadFile(stream).ConfigureAwait(false);
        }

        public async Task SaveEtcCsvbAsync(Stream stream, EtcFgHcmHgCsvb etcCsvb)
        {
            await _etcWriter.WriteFile(stream, etcCsvb).ConfigureAwait(false);
        }
    }
}
