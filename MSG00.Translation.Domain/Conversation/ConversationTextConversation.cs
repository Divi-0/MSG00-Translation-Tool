using System.Collections.ObjectModel;

namespace MSG00.Translation.Infrastructure.Domain.Conversation
{
    public class ConversationTextConversation
    {
        public ObservableCollection<ConversationTextBox> TextBoxes { get; set; } = new ObservableCollection<ConversationTextBox>();
    }
}
