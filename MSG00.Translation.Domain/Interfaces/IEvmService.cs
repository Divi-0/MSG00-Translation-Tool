using MSG00.Translation.Domain.Evm;

namespace MSG00.Translation.Domain.Interfaces
{
    public interface IEvmService
    {
        Task<EvmBaseCsvb> GetEvmAsync(Stream stream);
        Task SaveEvmAsync(Stream stream, EvmBaseCsvb prologueCsvb);
    }
}
