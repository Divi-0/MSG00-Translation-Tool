using MSG00.Translation.Infrastructure.Domain.Misc;
using MSG00.Translation.Infrastructure.Domain.Prologue.Enums;

namespace MSG00.Translation.Infrastructure.Domain.Prologue
{
    public class ProloguePointer : NotifyBase
    {
        public required ProloguePointerType Type { get; set; }
        public required long OffsetValue { get; set; }
        public List<ProloguePointer> SubPointer { get; set; } = new List<ProloguePointer>();
    }
}
