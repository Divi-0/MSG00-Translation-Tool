using MSG00.Translation.Infrastructure.Domain.Etc;

namespace MSG00.Translation.Infrastructure.Domain.Interfaces
{
    public interface IEtcFgHcmHgService
    {
        Task<EtcFgHcmHgCsvb> GetEtcCsvbAsync(Stream stream);
        Task SaveEtcCsvbAsync(Stream stream, EtcFgHcmHgCsvb etcCsvb);
    }
}
