using MSG00.Translation.Domain.EvmBase;
using MSG00.Translation.Domain.Files.Csvb;

namespace MSG00.Translation.Infrastructure.Reader.EvmBase
{
    public interface IEvmReader
    {
        Task<EvmBaseCsvb> ReadAsync(CsvbFile csvbFile, Stream streaml, CancellationToken cancellationToken = default);
    }
}
