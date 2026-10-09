using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue
{
    public class ProloguePointer : NotifyBase
    {
        public required ProloguePointerType Type { get; set; }
        public required long OffsetValue { get; set; }
        public List<ProloguePointer> SubPointer { get; set; } = new List<ProloguePointer>();

        /// <summary>false when the file ends before this pointer's slot (last group of the Epilogues); nothing is written to the offset table then</summary>
        public bool HasSlot { get; set; } = true;
    }
}
