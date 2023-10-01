using MSG00.Translation.Infrastructure.Domain.StaffRoll;

namespace MSG00.Translation.Infrastructure.Writer.StaffRoll
{
    internal interface IStaffRollWriter
    {
        Task WriteFile(Stream stream, StaffRollCsvb staffRollCsvb);
    }
}
