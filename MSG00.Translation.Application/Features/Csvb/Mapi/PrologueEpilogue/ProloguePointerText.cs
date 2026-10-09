using MSG00.Translation.Application.Features.Csvb;
using System.Collections.ObjectModel;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.PrologueEpilogue
{
    public class ProloguePointerText : ProloguePointer
    {
        public ProloguePointerText()
        {
            Type = ProloguePointerType.Text;
        }

        public string TextLines { get; set; } = string.Empty;

    }
}
