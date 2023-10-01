using MSG00.Translation.Infrastructure.Domain.Epilogue;

namespace MSG00.Translation.Infrastructure.Reader.Epilogue
{
    internal interface IEpilogueReader
    {
        Task<EpilogueCsvb> ReadFile(Stream stream);
    }
}
