using MSG00.Translation.Infrastructure.Domain.Interfaces;
using MSG00.Translation.Infrastructure.Domain.Requirement;
using MSG00.Translation.Infrastructure.Reader.Requirement;
using MSG00.Translation.Infrastructure.Writer.Requirement;

namespace MSG00.Translation.Infrastructure.Services
{
    internal class RequirementService : IRequirementService
    {
        private readonly IRequirementReader _requirementReader;
        private readonly IRequirementWriter _requirementWriter;

        public RequirementService(IRequirementReader requirementReader, IRequirementWriter requirementWriter)
        {
            _requirementReader = requirementReader;
            _requirementWriter = requirementWriter;
        }

        public async Task<RequirementCsvb> GetRequirementAsync(Stream stream)
        {
            return await _requirementReader.ReadFile(stream).ConfigureAwait(false);
        }

        public async Task SaveRequirementAsync(Stream stream, RequirementCsvb requirementCsvb)
        {
            await _requirementWriter.WriteFile(stream, requirementCsvb).ConfigureAwait(false);
        }
    }
}
