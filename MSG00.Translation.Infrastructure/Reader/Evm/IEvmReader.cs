using MSG00.Translation.Domain.Evm;

namespace MSG00.Translation.Infrastructure.Reader.Evm
{
    public interface IEvmReader
    {
        Task<EvmCsvb> ReadFile(Stream stream);
    }
}
