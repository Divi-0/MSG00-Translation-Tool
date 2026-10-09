using Mediator;
using MSG00.Translation.Application.Features.Csvb.EvmBase;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.EvmBase.Read
{
    public sealed record ReadEvmBase(CsvbFile CsvbFile, Stream Stream) : IRequest<EvmBaseCsvb>
    {
    }
}
