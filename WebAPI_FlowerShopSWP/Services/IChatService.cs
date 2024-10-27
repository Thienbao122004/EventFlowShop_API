using WebAPI_FlowerShopSWP.DTO;

namespace WebAPI_FlowerShopSWP.Services
{
    public interface IChatService
    {
        Task<ConversationResponseDto> CreateConversation(int currentUserId, CreateConversationDto dto);
        Task<MessageResponseDto> SendMessage(int senderId, SendMessageDto dto);
        Task<ChatHistoryResponseDto> GetChatHistory(int currentUserId, int conversationId);
        Task<List<ConversationResponseDto>> GetUserConversations(int userId);
        Task<int> GetUnreadMessageCount(int userId);
        Task MarkMessagesAsRead(int conversationId, int userId);
    }
}
