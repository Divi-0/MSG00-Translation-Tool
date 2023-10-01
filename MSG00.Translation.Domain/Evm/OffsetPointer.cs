using MSG00.Translation.Infrastructure.Domain.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSG00.Translation.Domain.Evm
{
    public class OffsetPointer : NotifyBase
    {
        public required int Value { get; set; }
    }
}
