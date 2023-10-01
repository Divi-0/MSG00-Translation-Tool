using MSG00.Translation.Infrastructure.Domain.Requirement;

namespace MSG00.Translation.Infrastructure.Writer.Requirement
{
    internal interface IRequirementWriter
    {
        Task WriteFile(Stream stream, RequirementCsvb etcCsvb);
    }
}
