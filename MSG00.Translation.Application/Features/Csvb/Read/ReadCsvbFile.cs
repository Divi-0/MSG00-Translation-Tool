using Mediator;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.Read
{
    public sealed record ReadCsvbFile(Stream FileStream) : IRequest<CsvbFile>
    {
    }
}
