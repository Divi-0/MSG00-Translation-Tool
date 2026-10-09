using MSG00.Translation.Application.Features.Csvb.Mapi;

namespace MSG00.Translation.Application.Features.Csvb.Mapi.Conversation
{
    public class ConversationCsvb : MapiCsvb
    {
        public required byte[] AfterTextSectionBytes { get; init; }

        /// <summary>
        /// bytes at the start of the offset table before the first slot (e.g. 0A 00 00 00), meaning unknown
        /// </summary>
        public required byte[] OffsetTablePrefix { get; init; }

        public List<ConversationPointer> Pointers { get; set; } = new List<ConversationPointer>();

        /// <summary>
        /// Adds a new text box directly after the group of <paramref name="text"/>; it is shown by the same character.
        /// </summary>
        public ConversationPointerText InsertTextAfter(ConversationPointerText text, string textLines)
        {
            var index = IndexOfGroup(text);
            var newText = new ConversationPointerText
            {
                Type = ConversationPointerType.Text,
                Slot = new byte[4],
                TextLines = textLines,
            };

            Pointers.Insert(index + 1, newText);
            return newText;
        }

        /// <summary>
        /// Removes a text box that is not combined with other commands (e.g. the start of a yes/no branch).
        /// </summary>
        public bool CanRemove(ConversationPointerText text) =>
            text.Type == ConversationPointerType.Text && text.SubPointer.Count == 0 && Pointers.Contains(text);

        public void Remove(ConversationPointerText text)
        {
            if (!CanRemove(text))
            {
                throw new InvalidOperationException("Only plain text boxes can be removed");
            }

            Pointers.Remove(text);
        }

        private int IndexOfGroup(ConversationPointer pointer)
        {
            var index = Pointers.FindIndex(x => x.Group.Contains(pointer));
            return index >= 0 ? index : throw new ArgumentException("Pointer is not part of this conversation", nameof(pointer));
        }
    }
}