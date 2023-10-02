using Mediator;
using MSG00.Translation.Domain.EvmBase;
using MSG00.Translation.Domain.Files.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.EvmBase.Read
{
    public sealed record ReadEvmBase(CsvbFile CsvbFile, Stream Stream) : IRequest<EvmBaseCsvb>
    {
    }
}
