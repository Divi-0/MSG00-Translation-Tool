using MSG00.Translation.Domain.Evm;

namespace MSG00.Translation.Domain.Interfaces
{
    public interface IEvmService
    {
        Task<EvmCsvb> GetEvmAsync(Stream stream);
        Task SaveEvmAsync(Stream stream, EvmCsvb prologueCsvb);
    }
}
