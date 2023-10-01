using MSG00.Translation.Infrastructure.Domain.Interfaces;
using MSG00.Translation.Infrastructure.Domain.Prologue;
using MSG00.Translation.Infrastructure.Reader.Prologue;
using MSG00.Translation.Infrastructure.Writer.ProEpilogue;

namespace MSG00.Translation.Infrastructure.Services
{
    internal class PrologueService : IPrologueService
    {
        private readonly IPrologueReader _prologueReader;
        private readonly IPrologueWriter _proEpilogueWriter;

        public PrologueService(IPrologueReader prologueReader, IPrologueWriter proEpilogueWriter)
        {
            _prologueReader = prologueReader;
            _proEpilogueWriter = proEpilogueWriter;
        }

        public async Task<PrologueCsvb> GetPrologueAsync(Stream stream)
        {
            return await _prologueReader.ReadFile(stream).ConfigureAwait(false);
        }

        public async Task SavePrologueAsync(Stream stream, PrologueCsvb prologueCsvb)
        {
            await _proEpilogueWriter.WriteFile(stream, prologueCsvb).ConfigureAwait(false);
        }
    }
}
