using Mediator;
using MSG00.Translation.Application.Features.Csvb.EvmBase.Read;
using MSG00.Translation.Application.Features.Csvb.Mapi;
using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.Read
{
    public class ReadCsvbFileHandler : IRequestHandler<ReadCsvbFile, CsvbFile>
    {
        private readonly ICsvbReader _csvbReader;
        private readonly IMapiService _mapiService;
        private readonly IMediator _mediator;
        private readonly Table.IReader _tableReader;
        private readonly CutIn.IReader _cutInReader;

        public ReadCsvbFileHandler(IMediator mediator, ICsvbReader csvbReader, IMapiService mapiService, Table.IReader tableReader, CutIn.IReader cutInReader)
        {
            _mediator = mediator;
            _csvbReader = csvbReader;
            _mapiService = mapiService;
            _tableReader = tableReader;
            _cutInReader = cutInReader;
        }

        public async ValueTask<CsvbFile> Handle(ReadCsvbFile request, CancellationToken cancellationToken = default)
        {
            try
            {
                var csvbFile = await _csvbReader.ReadAsync(request.FileStream, cancellationToken);

                switch (csvbFile.Type)
                {
                    case CsvbFileType.MAPI:
                        csvbFile = await _mapiService.GetMapiCsvbAsync(csvbFile, request.FileStream).ConfigureAwait(false);
                        break;
                    case CsvbFileType.EVM_BASE:
                        csvbFile = await _mediator.Send(new ReadEvmBase(csvbFile, request.FileStream), cancellationToken);
                        break;
                    case CsvbFileType.BLOCK:
                    case CsvbFileType.OG_TABLE:
                        csvbFile = await _tableReader.ReadFileAsync(csvbFile, request.FileStream).ConfigureAwait(false);
                        break;
                    case CsvbFileType.CUT_IN:
                        csvbFile = await _cutInReader.ReadFileAsync(csvbFile, request.FileStream).ConfigureAwait(false);
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
