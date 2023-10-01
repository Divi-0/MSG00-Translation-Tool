using MSG00.Translation.Infrastructure.Domain.Conversation.Enums;
using System.Collections.ObjectModel;

namespace MSG00.Translation.Infrastructure.Domain.Conversation
{
    public sealed class ConversationPointerCharacter : ConversationPointer
    {
        public ConversationPointerCharacter()
        {
            TextBoxes = new ObservableCollection<ConversationPointerText>();
        }

        public long OffsetValue { get; set; }

        private ObservableCollection<ConversationPointerText> _textBoxes;
        public ObservableCollection<ConversationPointerText> TextBoxes
        {
            get { return _textBoxes; }
            set { SetProperty(ref _textBoxes, value); }
        }

        /// <summary>
        /// is infront of the start of the character pointer and has the same value
        /// </summary>
        public ConversationPointerType? ExtraType { get; set; } = null;
    }
}
