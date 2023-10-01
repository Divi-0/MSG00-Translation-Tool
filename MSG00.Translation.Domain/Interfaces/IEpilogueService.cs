using MSG00.Translation.Infrastructure.Domain.Epilogue;

namespace MSG00.Translation.Infrastructure.Domain.Interfaces
{
    public interface IEpilogueService
    {
        Task<EpilogueCsvb> GetPrologueAsync(Stream stream);
        Task SavePrologueAsync(Stream stream, EpilogueCsvb prologueCsvb);
    }
}
