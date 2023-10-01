using MSG00.Translation.Infrastructure.Domain.Conversation;

namespace MSG00.Translation.Infrastructure.Domain.Interfaces
{
    public interface IConversationService
    {
        Task<ConversationCsvb> GetFile(Stream fileStream);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="conversationCsvb"></param>
        /// <returns>path of backup file</returns>
        Task SaveFile(Stream stream, ConversationCsvb conversationCsvb);
    }
}
