using Mediator;
using MSG00.Translation.Domain.EvmBase;

namespace MSG00.Translation.Application.Features.Csvb.EvmBase.Read
{
    public class ReadEvmBaseHandler : IRequestHandler<ReadEvmBase, EvmBaseCsvb>
    {
        public ValueTask<EvmBaseCsvb> Handle(ReadEvmBase request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
