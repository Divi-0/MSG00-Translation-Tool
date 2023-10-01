namespace MSG00.Translation.Infrastructure.Domain.Conversation
{
    public class ConversationPointerTextWithChangingCharImage : ConversationPointerText
    {
        public ConversationPointerTextWithChangingCharImage()
        {
            PointerItemType = Enums.ConversationPointerTextType.TextWithChangingCharImage;
        }

        public ConversationPointerTextWithChangingCharImage(ConversationPointerText original) : base(original)
        {
            PointerItemType = Enums.ConversationPointerTextType.TextWithChangingCharImage;
        }

        public List<byte> ChangingCharImageOffsetBytes { get; set; } = new List<byte>();
    }
}
