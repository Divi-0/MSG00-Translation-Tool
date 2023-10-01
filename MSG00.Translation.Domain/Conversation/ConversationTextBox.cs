using System.Collections.ObjectModel;
using MSG00.Translation.Infrastructure.Domain.Conversation.Enums;
using MSG00.Translation.Infrastructure.Domain.Shared;

namespace MSG00.Translation.Infrastructure.Domain.Conversation
{
    public class ConversationTextBox
    {
        public ConversationItemLifeTime Type { get; set; }
        public string Title { get; set; }
        public int OriginalByteLengthWithZeros { get; set; }
        public ObservableCollection<CsvbTextLine> Lines { get; set; } = new ObservableCollection<CsvbTextLine>();
    }
}
