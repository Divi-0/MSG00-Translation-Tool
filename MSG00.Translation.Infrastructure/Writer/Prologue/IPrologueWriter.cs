using MSG00.Translation.Infrastructure.Domain.Prologue;

namespace MSG00.Translation.Infrastructure.Writer.ProEpilogue
{
    internal interface IPrologueWriter
    {
        Task WriteFile(Stream stream, PrologueCsvb proEpilogueCsvb);
    }
}
