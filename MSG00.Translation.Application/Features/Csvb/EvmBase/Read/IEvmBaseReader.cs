using MSG00.Translation.Application.Features.Csvb.EvmBase;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.EvmBase.Read
{
    public interface IEvmBaseReader
    {
        Task<EvmBaseCsvb> ReadAsync(CsvbFile csvbFile, Stream streaml, CancellationToken cancellationToken = default);
    }
}
