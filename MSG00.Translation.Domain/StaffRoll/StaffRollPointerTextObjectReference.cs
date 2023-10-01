using MSG00.Translation.Infrastructure.Domain.Epilogue.Enums;
using MSG00.Translation.Infrastructure.Domain.StaffRoll.Enums;

namespace MSG00.Translation.Infrastructure.Domain.StaffRoll
{
    public class StaffRollPointerTextObjectReference : StaffRollPointer
    {
        public StaffRollPointerTextObjectReference()
        {
            Type = StaffRollPointerType.TextObjectReference;
        }

        public List<string> TextReferences { get; set; } = new List<string>();
    }
}
