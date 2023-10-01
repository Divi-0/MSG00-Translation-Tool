using System.Collections.ObjectModel;
using MSG00.Translation.Infrastructure.Domain.Conversation.Enums;
using MSG00.Translation.Infrastructure.Domain.Shared;

namespace MSG00.Translation.Infrastructure.Domain.Conversation
{
    public class ConversationPointerText : ConversationPointer
    {
        public ConversationPointerText()
        {
            PointerItemType = ConversationPointerTextType.Normal;

            Lines = new ObservableCollection<CsvbTextLine>();
        }

        public ConversationPointerText(ConversationPointerText original)
        {
            Type = original.Type;

            ItemLifeTime = original.ItemLifeTime;
            PointerItemType = original.PointerItemType;
            OffsetValue = original.OffsetValue;
            Lines = original.Lines;
            OriginalByteLengthWithZeros = original.OriginalByteLengthWithZeros;
            Title = original.Title;
        }

        public required ConversationItemLifeTime ItemLifeTime { get; set; }
        public ConversationPointerTextType PointerItemType { get; protected init; }
        public int OffsetValue { get; set; }

        private ObservableCollection<CsvbTextLine> _lines;
        public ObservableCollection<CsvbTextLine> Lines
        {
            get { return _lines; }
            set { SetProperty(ref _lines, value); }
        }

        public int OriginalByteLengthWithZeros { get; set; }
        public string Title { get; init; }
    }
}
