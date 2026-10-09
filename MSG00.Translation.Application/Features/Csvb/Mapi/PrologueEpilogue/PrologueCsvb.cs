using MSG00.Translation.Application.Features.Csvb.Mapi;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue
{
    public class PrologueCsvb : MapiCsvb
    {
        public required byte[] AfterTextSectionBytes { get; init; }
        public List<ProloguePointer> ProEpiloguePointers { get; set; } = new List<ProloguePointer>();
    }
}
