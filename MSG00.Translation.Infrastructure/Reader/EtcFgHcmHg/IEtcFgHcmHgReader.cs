using MSG00.Translation.Infrastructure.Domain.Etc;

namespace MSG00.Translation.Infrastructure.Reader.EtcFgHcmHg
{
    internal interface IEtcFgHcmHgReader
    {
        Task<EtcFgHcmHgCsvb> ReadFile(Stream stream);
    }
}
