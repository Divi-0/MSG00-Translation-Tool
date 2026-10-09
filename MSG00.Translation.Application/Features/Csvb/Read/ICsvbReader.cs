using MSG00.Translation.Application.Features.Csvb;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSG00.Translation.Application.Features.Csvb.Read
{
    public interface ICsvbReader
    {
        Task<CsvbFile> ReadAsync(Stream stream, CancellationToken cancellationToken = default);
    }
}
