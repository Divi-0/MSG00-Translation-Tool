namespace MSG00.Translation.Infrastructure.Domain.Conversation
{
    public class ConversationOffset
    {
        /// <summary>
        /// 8 byte long
        /// </summary>
        public long CharacterOffset { get; set; }
        /// <summary>
        /// 4 byte long
        /// </summary>
        public List<int> TextBoxesOffsets { get; set; } = new List<int>();
    }
}
