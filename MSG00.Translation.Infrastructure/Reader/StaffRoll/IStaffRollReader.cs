using MSG00.Translation.Infrastructure.Domain.StaffRoll;

namespace MSG00.Translation.Infrastructure.Reader.StaffRoll
{
    internal interface IStaffRollReader
    {
        Task<StaffRollCsvb> ReadFile(Stream stream);
    }
}
