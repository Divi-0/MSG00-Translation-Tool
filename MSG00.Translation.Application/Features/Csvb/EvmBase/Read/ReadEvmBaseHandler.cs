using Mediator;
using MSG00.Translation.Domain.EvmBase;
using MSG00.Translation.Infrastructure.Reader.EvmBase;

namespace MSG00.Translation.Application.Features.Csvb.EvmBase.Read
{
    public class ReadEvmBaseHandler : IRequestHandler<ReadEvmBase, EvmBaseCsvb>
    {
        private readonly IEvmBaseReader _evmBaseReader;

        public ReadEvmBaseHandler(IEvmBaseReader evmBaseReader)
        {
            _evmBaseReader = evmBaseReader;
        }

        public async ValueTask<EvmBaseCsvb> Handle(ReadEvmBase request, CancellationToken cancellationToken)
        {
            return await _evmBaseReader.ReadAsync(request.CsvbFile, request.Stream, cancellationToken);
        }
    }
}
