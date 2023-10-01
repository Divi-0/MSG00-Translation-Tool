using MSG00.Translation.Infrastructure.Domain.Epilogue.Enums;

namespace MSG00.Translation.Infrastructure.Domain.Epilogue
{
    public class EpiloguePointerTextObjectReference : EpiloguePointer
    {
        public EpiloguePointerTextObjectReference()
        {
            Type = EpiloguePointerType.TextObjectReference;
        }

        public List<string> TextReferences { get; set; } = new List<string>();
    }
}
