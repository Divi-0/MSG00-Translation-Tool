using MSG00.Translation.Infrastructure.Domain.Misc;
using MSG00.Translation.Infrastructure.Domain.StaffRoll.Enums;

namespace MSG00.Translation.Infrastructure.Domain.StaffRoll
{
    public class StaffRollPointer : NotifyBase
    {
        public required StaffRollPointerType Type { get; set; }
        public required long OffsetValue { get; set; }
        public List<StaffRollPointer> SubPointer { get; set; } = new List<StaffRollPointer>();
    }
}
