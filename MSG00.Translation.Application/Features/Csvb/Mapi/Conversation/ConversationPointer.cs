using MSG00.Translation.Application.Features.Csvb;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.Conversation
{
    public class ConversationPointer : NotifyBase
    {
        public required ConversationPointerType Type { get; set; }

        /// <summary>
        /// raw offset table slot of the group; set on the first pointer of a group only, empty for sub pointers
        /// </summary>
        public byte[] Slot { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// pointers sharing the slot of this pointer
        /// </summary>
        public List<ConversationPointer> SubPointer { get; set; } = new List<ConversationPointer>();

        public IEnumerable<ConversationPointer> Group => SubPointer.Prepend(this);
    }
}