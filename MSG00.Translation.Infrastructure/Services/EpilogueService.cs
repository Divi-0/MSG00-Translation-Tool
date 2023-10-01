using MSG00.Translation.Infrastructure.Domain.Epilogue;
using MSG00.Translation.Infrastructure.Domain.Interfaces;
using MSG00.Translation.Infrastructure.Reader.Epilogue;
using MSG00.Translation.Infrastructure.Writer.Epilogue;

namespace MSG00.Translation.Infrastructure.Services
{
    internal class EpilogueService : IEpilogueService
    {
        private readonly IEpilogueReader _epilogueReader;
        private readonly IEpilogueWriter _epilogueWriter;

        public EpilogueService(IEpilogueReader epilogueReader, IEpilogueWriter epilogueWriter)
        {
            _epilogueReader = epilogueReader;
            _epilogueWriter = epilogueWriter;
        }

        public async Task<EpilogueCsvb> GetPrologueAsync(Stream stream)
        {
            return await _epilogueReader.ReadFile(stream).ConfigureAwait(false);
        }

        public async Task SavePrologueAsync(Stream stream, EpilogueCsvb epilogueCsvb)
        {
            await _epilogueWriter.WriteFile(stream, epilogueCsvb).ConfigureAwait(false);
        }
    }
}
