using MSG00.Translation.Infrastructure.Domain.Conversation.Enums;
using MSG00.Translation.Infrastructure.Domain.Misc;

namespace MSG00.Translation.Infrastructure.Domain.Conversation
{
    public abstract class ConversationPointer : NotifyBase
    {
        public required ConversationPointerType Type { get; set; }
    }
}
