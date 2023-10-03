using MSG00.Translation.Domain.EvmBase;
using MSG00.Translation.Domain.Interfaces;
using MSG00.Translation.Infrastructure.Reader.EvmBase;

namespace MSG00.Translation.Infrastructure.Services
{
    public class EvmService : IEvmService
    {
        private readonly IEvmBaseReader _evmReader;

        public EvmService(IEvmBaseReader evmReader)
        {
            _evmReader = evmReader;
        }

        public Task<EvmBaseCsvb> GetEvmAsync(Stream stream)
        {
            try
            {
                return null;
            }
            catch (Exception e)
            {

                throw;
            }
        }

        public Task SaveEvmAsync(Stream stream, EvmBaseCsvb prologueCsvb)
        {
            throw new NotImplementedException();
        }
    }
}
