using Mediator;
using MSG00.Translation.Domain.Files.Csvb;
using MSG00.Translation.Infrastructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSG00.Translation.Application.Features.Csvb.Read
{
    public class ReadCsvbFileHandler : IRequestHandler<ReadCsvbFile, CsvbFile>
    {
        private readonly ICsvbReader _csvbReader;

        public ReadCsvbFileHandler(ICsvbReader csvbReader)
        {
            this._csvbReader = csvbReader;
        }

        public async ValueTask<CsvbFile> Handle(ReadCsvbFile request, CancellationToken cancellationToken = default)
        {
            var csvbFile = await _csvbReader.ReadAsync(request.FileStream, cancellationToken);

            return csvbFile;
        }
    }
}
