using MSG00.Translation.Infrastructure.Domain.Etc;

namespace MSG00.Translation.Infrastructure.Writer.Etc
{
    internal interface IEtcFgHcmHgWriter
    {
        Task WriteFile(Stream stream, EtcFgHcmHgCsvb etcCsvb);
    }
}
