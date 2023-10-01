using MSG00.Translation.Domain.Files.Csvb;
using MSG00.Translation.Infrastructure.Domain.Enums;

namespace MSG00.Translation.Infrastructure.Domain.Requirement
{
    public sealed class RequirementCsvb : CsvbFile
    {
        public RequirementCsvb()
        {
            Type = CsvbFileType.BLOCK;
        }

        public IReadOnlyList<RequirementPointer> Pointers { get; init; } = new List<RequirementPointer>();
    }
}
