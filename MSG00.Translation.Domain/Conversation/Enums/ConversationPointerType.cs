namespace MSG00.Translation.Infrastructure.Domain.Conversation.Enums
{
    public enum ConversationPointerType
    {
        CharacterImage = 0x00,
        Text = 0x01,
        YesNoSelect = 0x05,
        YesNoSelectYesStart = 0x09,
        YesNoSelectNoStart = 0x0A,
        YesNoSelectYesOrNoEnd = 0x0D,
        Alert = 0x0E,
        EmptyTextBox = 0x10,
        TextWithUnknownOffset = 0x03
    }
}
