using MSG00.Translation.Infrastructure.Domain.Epilogue;

namespace MSG00.Translation.Infrastructure.Writer.Epilogue
{
    internal interface IEpilogueWriter
    {
        Task WriteFile(Stream stream, EpilogueCsvb epilogueCsvb);
    }
}
