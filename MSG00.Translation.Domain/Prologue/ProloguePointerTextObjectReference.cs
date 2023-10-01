using MSG00.Translation.Infrastructure.Domain.Prologue;
using MSG00.Translation.Infrastructure.Domain.Prologue.Enums;

namespace MSG00.Translation.Infrastructure.Domain.Prologue
{
    public class ProloguePointerTextObjectReference : ProloguePointer
    {
        public ProloguePointerTextObjectReference()
        {
            Type = ProloguePointerType.TextObjectReference;
        }

        public List<string> TextReferences { get; set; } = new List<string>();
    }
}
