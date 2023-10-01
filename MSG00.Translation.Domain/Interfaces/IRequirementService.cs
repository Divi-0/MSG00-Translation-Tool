using MSG00.Translation.Infrastructure.Domain.Requirement;

namespace MSG00.Translation.Infrastructure.Domain.Interfaces
{
    public interface IRequirementService
    {
        Task<RequirementCsvb> GetRequirementAsync(Stream stream);
        Task SaveRequirementAsync(Stream stream, RequirementCsvb requirementCsvb);
    }
}
