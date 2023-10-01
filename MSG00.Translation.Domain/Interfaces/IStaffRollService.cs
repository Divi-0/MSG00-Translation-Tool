using MSG00.Translation.Infrastructure.Domain.StaffRoll;

namespace MSG00.Translation.Infrastructure.Domain.Interfaces
{
    public interface IStaffRollService
    {
        Task<StaffRollCsvb> GetStaffRollAsync(Stream stream);
        Task SaveStaffRollAsync(Stream stream, StaffRollCsvb staffRollCsvb);
    }
}
