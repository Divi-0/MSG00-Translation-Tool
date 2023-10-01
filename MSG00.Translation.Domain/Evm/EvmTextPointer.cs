using MSG00.Translation.Infrastructure.Domain.Shared;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSG00.Translation.Domain.Evm
{
    public class EvmTextPointer : EvmPointer
    {
        public required ObservableCollection<CsvbTextLine> TextBox { get; set; }
    }
}
