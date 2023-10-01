using MSG00.Translation.Domain.Files.Csvb;
using MSG00.Translation.Infrastructure.Domain.Enums;

namespace MSG00.Translation.Infrastructure.Domain.StaffRoll
{
    public class StaffRollCsvb : CsvbFile
    {
        public StaffRollCsvb()
        {
            Type = CsvbFileType.MAPI;
        }
        public required byte[] MapiHeaderBytes { get; init; }
        public required byte[] AfterTextSectionBytes { get; init; }
        public required int CountOfPointersInFile { get; set; }
        public required int FileSizeToTextEnd { get; set; }
        public required int FileSizeWithUnimportantInfo { get; set; }
        public List<StaffRollPointer> Pointers { get; set; } = new List<StaffRollPointer>();
    }
}
