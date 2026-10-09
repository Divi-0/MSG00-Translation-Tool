namespace MSG00.Translation.Application.Features.Csvb.Mapi;

public enum MapiType
{
    /// <summary>2D/Conv/C_error.csvb: a conversation without the type in row 0 (one text box, shown for a missing conversation)</summary>
    ConversationWithoutType = 0x00,
    Conversation = 0x10,
    PrologueEpilogue = 0x0A,
    StaffRoll = 0x0D,
}