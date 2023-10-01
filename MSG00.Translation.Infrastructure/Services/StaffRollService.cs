using MSG00.Translation.Infrastructure.Domain.Interfaces;
using MSG00.Translation.Infrastructure.Domain.StaffRoll;
using MSG00.Translation.Infrastructure.Reader.StaffRoll;
using MSG00.Translation.Infrastructure.Writer.StaffRoll;

namespace MSG00.Translation.Infrastructure.Services
{
    internal class StaffRollService : IStaffRollService
    {
        private readonly IStaffRollReader _staffRollReader;
        private readonly IStaffRollWriter _staffRollWriter;

        public StaffRollService(IStaffRollReader staffRollReader, IStaffRollWriter staffRollWriter)
        {
            _staffRollReader = staffRollReader;
            _staffRollWriter = staffRollWriter;
        }

        public async Task<StaffRollCsvb> GetStaffRollAsync(Stream stream)
        {
            return await _staffRollReader.ReadFile(stream).ConfigureAwait(false);
        }

        public async Task SaveStaffRollAsync(Stream stream, StaffRollCsvb staffRollCsvb)
        {
            await _staffRollWriter.WriteFile(stream, staffRollCsvb).ConfigureAwait(false);
        }
    }
}
