using MSG00.Translation.Infrastructure.Domain.Requirement;

namespace MSG00.Translation.Infrastructure.Reader.Requirement
{
    internal interface IRequirementReader
    {
        Task<RequirementCsvb> ReadFile(Stream stream);
    }
}
