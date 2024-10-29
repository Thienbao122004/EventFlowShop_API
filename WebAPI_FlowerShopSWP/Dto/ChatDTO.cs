namespace WebAPI_FlowerShopSWP.DTO
{
    public class CreateConversationDto
    {
        public int SellerId { get; set; }
    }

    public class SendMessageDto
    {
        public int ConversationId { get; set; }
        public string? MessageContent { get; set; }
        public IFormFile? Image { get; set; }
    }

    public class MessageResponseDto
    {
        public int MessageId { get; set; }
        public int SenderId { get; set; }
        public string? MessageContent { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime SendTime { get; set; }
        public bool IsRead { get; set; }
        public string SenderName { get; set; }
        public string? SenderAvatar { get; set; }
    }

    public class ConversationResponseDto
    {
        public int ConversationId { get; set; }
        public UserInfoDto Seller { get; set; }
        public UserInfoDto Buyer { get; set; }
        public DateTime UpdatedAt { get; set; }
        public MessageResponseDto LastMessage { get; set; }
    }

    public class UserInfoDto
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string Avatar { get; set; }
    }

    public class ChatHistoryResponseDto
    {
        public ConversationResponseDto Conversation { get; set; }
        public List<MessageResponseDto> Messages { get; set; }
    }

    namespace WebAPI_FlowerShopSWP.Exceptions
    {
        public class NotFoundException : Exception
        {
            public NotFoundException(string message) : base(message) { }
        }

        public class UnauthorizedException : Exception
        {
            public UnauthorizedException(string message) : base(message) { }
        }
    }
}
