using MSG00.Translation.Infrastructure.Domain.Prologue;

namespace MSG00.Translation.Infrastructure.Domain.Interfaces
{
    public interface IPrologueService
    {
        Task<PrologueCsvb> GetPrologueAsync(Stream stream);
        Task SavePrologueAsync(Stream stream, PrologueCsvb prologueCsvb);
    }
}
