using MSG00.Translation.Infrastructure.Domain.Epilogue.Enums;
using MSG00.Translation.Infrastructure.Domain.Misc;

namespace MSG00.Translation.Infrastructure.Domain.Epilogue
{
    public class EpiloguePointer : NotifyBase
    {
        public required EpiloguePointerType Type { get; set; }
        public required long OffsetValue { get; set; }
        public List<EpiloguePointer> SubPointer { get; set; } = new List<EpiloguePointer>();
    }
}
