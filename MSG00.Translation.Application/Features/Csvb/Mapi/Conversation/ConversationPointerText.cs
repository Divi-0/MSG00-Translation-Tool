namespace MSG00.Translation.Application.Features.Csvb.Mapi.Conversation
{
    /// <summary>
    /// Pointer whose group slot starts with a text offset (<see cref="ConversationPointerType.Text"/>,
    /// <see cref="ConversationPointerType.TextWithParameters"/>, <see cref="ConversationPointerType.Question"/>).
    /// </summary>
    public class ConversationPointerText : ConversationPointer
    {
        public string TextLines { get; set; } = string.Empty;
    }
}