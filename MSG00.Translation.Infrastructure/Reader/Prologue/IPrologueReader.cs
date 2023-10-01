using MSG00.Translation.Infrastructure.Domain.Prologue;

namespace MSG00.Translation.Infrastructure.Reader.Prologue
{
    internal interface IPrologueReader
    {
        Task<PrologueCsvb> ReadFile(Stream stream);
    }
}
