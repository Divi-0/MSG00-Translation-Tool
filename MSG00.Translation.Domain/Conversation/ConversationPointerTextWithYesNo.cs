using System.Collections.ObjectModel;
using MSG00.Translation.Infrastructure.Domain.Conversation.Enums;

namespace MSG00.Translation.Infrastructure.Domain.Conversation
{
    public sealed class ConversationPointerTextWithYesNo : ConversationPointerText
    {
        public ConversationPointerTextWithYesNo()
        {
            PointerItemType = ConversationPointerTextType.YesNo;

            YesPointer = new ObservableCollection<ConversationPointerCharacter>();
            NoPointer = new ObservableCollection<ConversationPointerCharacter>();
        }

        private ObservableCollection<ConversationPointerCharacter> _yesPointer = new ObservableCollection<ConversationPointerCharacter>();
        public ObservableCollection<ConversationPointerCharacter> YesPointer
        {
            get => _yesPointer;
            set => SetProperty(ref _yesPointer, value);
        }

        private ObservableCollection<ConversationPointerCharacter> _noPointer = new ObservableCollection<ConversationPointerCharacter>();
        public ObservableCollection<ConversationPointerCharacter> NoPointer
        {
            get => _noPointer;
            set => SetProperty(ref _noPointer, value);
        }
    }
}
