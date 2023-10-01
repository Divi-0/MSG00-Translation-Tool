using MSG00.Translation.Domain.Evm.Enums;
using MSG00.Translation.Infrastructure.Domain.Misc;

namespace MSG00.Translation.Domain.Evm
{
    public class EvmPointer : NotifyBase
    {
        public required EvmPointerType Type { get; set; }
        public required long OffsetPointerType { get; set; }
        public required long OffsetPointerAttribute { get; set; }
        public required long OffsetValue { get; set; }
    }
}
