using MSG00.Translation.Infrastructure.Domain.Shared;
using System.Collections.ObjectModel;

namespace MSG00.Translation.Infrastructure.Domain.StaffRoll
{
    public class StaffRollPointerText : StaffRollPointer
    {
        public StaffRollPointerText()
        {
            Type = Enums.StaffRollPointerType.Text;

            TextLines = new ObservableCollection<CsvbTextLine>();
        }

        private ObservableCollection<CsvbTextLine> _textLines;
        public ObservableCollection<CsvbTextLine> TextLines
        {
            get { return _textLines; }
            set { SetProperty(ref _textLines, value); }
        }
    }
}
