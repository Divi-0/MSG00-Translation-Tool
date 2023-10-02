using Mediator;
using MSG00.Translation.Application.Features.Csvb.EvmBase.Read;
using MSG00.Translation.Domain.Files.Csvb;
using MSG00.Translation.Infrastructure.Interfaces;

namespace MSG00.Translation.Application.Features.Csvb.Read
{
    public class ReadCsvbFileHandler : IRequestHandler<ReadCsvbFile, CsvbFile>
    {
        private readonly ICsvbReader _csvbReader;
        private readonly IMediator _mediator;

        public ReadCsvbFileHandler(IMediator mediator, ICsvbReader csvbReader)
        {
            _mediator = mediator;
            _csvbReader = csvbReader;
        }

        public async ValueTask<CsvbFile> Handle(ReadCsvbFile request, CancellationToken cancellationToken = default)
        {
            try
            {
                var csvbFile = await _csvbReader.ReadAsync(request.FileStream, cancellationToken);

                switch (csvbFile.Type)
                {
                    case Infrastructure.Domain.Enums.CsvbFileType.MAPI:
                        break;
                    case Infrastructure.Domain.Enums.CsvbFileType.BLOCK:
                        break;
                    case Infrastructure.Domain.Enums.CsvbFileType.EVM_BASE:
                        csvbFile = await _mediator.Send(new ReadEvmBase(csvbFile, request.FileStream), cancellationToken);
                        break;
                    default:
                        throw new NotImplementedException();
                }

                return csvbFile;
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
