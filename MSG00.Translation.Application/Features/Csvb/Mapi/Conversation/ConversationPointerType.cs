namespace MSG00.Translation.Application.Features.Csvb.Mapi.Conversation
{
    /// <summary>
    /// Script commands of a conversation csvb (MAPI type 0x10), see docs/conversation/README.md.
    /// Pointers with the same offset form one group and share one offset table slot.
    /// </summary>
    public enum ConversationPointerType
    {
        /// <summary>
        /// Shows a character; 8 byte slot: character id (1 Setsuna, 2 Lockon, 3 Allelujah, 4 Tieria, 5 Sumeragi, ...) and a second value (position/expression, hypothesis).
        /// </summary>
        Character = 0x00,

        /// <summary>
        /// Text box; 4 byte slot with the text offset.
        /// </summary>
        Text = 0x01,

        /// <summary>
        /// Text box with three more values; 16 byte slot: text offset + 3 values (seen once, meaning unknown).
        /// </summary>
        TextWithParameters = 0x03,

        /// <summary>
        /// Question text before a yes/no choice; 4 byte slot with the text offset.
        /// </summary>
        Question = 0x05,

        /// <summary>
        /// Start of the "yes" branch.
        /// </summary>
        YesBranch = 0x09,

        /// <summary>
        /// Start of the "no" branch.
        /// </summary>
        NoBranch = 0x0A,

        /// <summary>
        /// Seen once at the start of a conversation together with <see cref="Character"/>. Meaning unknown.
        /// </summary>
        Unknown0B = 0x0B,

        /// <summary>
        /// Seen once together with <see cref="BranchEnd"/> and <see cref="Character"/>. Meaning unknown.
        /// </summary>
        Unknown0C = 0x0C,

        /// <summary>
        /// End of a branch; the last one in a file has no slot data.
        /// </summary>
        BranchEnd = 0x0D,

        /// <summary>
        /// Appears at the start together with <see cref="Character"/> or <see cref="EmptyTextBox"/> (legacy name "Alert"). Meaning unknown.
        /// </summary>
        Unknown0E = 0x0E,

        /// <summary>
        /// 4 byte slot (legacy name "EmptyTextBox"). Meaning unknown.
        /// </summary>
        EmptyTextBox = 0x10,
    }
}