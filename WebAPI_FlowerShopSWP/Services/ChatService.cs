using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WebAPI_FlowerShopSWP.DTO;
using WebAPI_FlowerShopSWP.DTO.WebAPI_FlowerShopSWP.Exceptions;
using WebAPI_FlowerShopSWP.Enums;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Services
{
    public class ChatService : IChatService
    {
        private readonly FlowerEventShopsContext _context;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly INotificationService _notificationService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ChatService> _logger;

        public ChatService(
            FlowerEventShopsContext context,
            IHubContext<ChatHub> hubContext,
            INotificationService notificationService,
            IWebHostEnvironment environment,
            ILogger<ChatService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _notificationService = notificationService;
            _environment = environment;
            _logger = logger;
        }

        public async Task<ConversationResponseDto> CreateConversation(int currentUserId, CreateConversationDto dto)
        {
            var receiver = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == dto.SellerId);

            if (receiver == null)
            {
                throw new NotFoundException("Không tìm thấy người dùng");
            }

            var existingConversation = await _context.Conversations
                .Include(c => c.Seller)
                .Include(c => c.Buyer)
                .Include(c => c.LastMessage)
                .FirstOrDefaultAsync(c =>
                    (c.SellerId == dto.SellerId && c.BuyerId == currentUserId ||
                     c.SellerId == currentUserId && c.BuyerId == dto.SellerId) &&
                    c.IsActive);

            if (existingConversation != null)
            {
                return MapToConversationResponse(existingConversation);
            }

            // Tạo conversation mới
            var conversation = new Conversation
            {
                SellerId = dto.SellerId,
                BuyerId = currentUserId,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            await _context.Entry(conversation)
                .Reference(c => c.Seller)
                .LoadAsync();
            await _context.Entry(conversation)
                .Reference(c => c.Buyer)
                .LoadAsync();

            // Tạo notification
            await _notificationService.CreateNotification(new CreateNotificationDTO
            {
                UserId = dto.SellerId,
                Title = "Tin nhắn mới",
                Content = $"Bạn có cuộc trò chuyện mới",
                Type = NotificationType.NewMessage,
                RelatedId = conversation.ConversationId,
                RelatedType = RelatedType.Chat
            });

            return MapToConversationResponse(conversation);
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                    return null;

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
                var uploadPath = Path.Combine(_environment.WebRootPath, "chat-images");

                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Trả về chỉ tên file
                return $"/chat-images/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saving image: {ex.Message}");
                throw;
            }
        }
        private async Task SendMessageNotification(Message message, int receiverId, Conversation conversation)
        {
            try
            {
                var lastNotifiedMessage = await _context.Notifications
                    .Where(n => n.UserId == receiverId
                        && n.Type == NotificationType.NewMessage
                        && n.RelatedId == conversation.ConversationId)
                    .OrderByDescending(n => n.CreatedAt)
                    .FirstOrDefaultAsync();

                if (lastNotifiedMessage == null
                    || lastNotifiedMessage.IsRead
                    || (DateTime.UtcNow - lastNotifiedMessage.CreatedAt).TotalMinutes > 5)
                {
                    string role = message.Sender.UserType == "Seller" ? "người bán" : "người mua";
                    string notificationContent = $"Tin nhắn mới từ {role} {message.Sender.FullName}"; // Sử dụng FullName

                    await _notificationService.CreateNotification(new CreateNotificationDTO
                    {
                        UserId = receiverId,
                        Title = "Tin nhắn mới",
                        Content = notificationContent,
                        Type = NotificationType.NewMessage,
                        RelatedId = conversation.ConversationId,
                        RelatedType = RelatedType.Chat
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending notification: {ex.Message}");
            }
        }

        public async Task<MessageResponseDto> SendMessage(int senderId, SendMessageDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var conversation = await _context.Conversations
                    .Include(c => c.Messages)
                    .FirstOrDefaultAsync(c => c.ConversationId == dto.ConversationId);

                if (conversation == null)
                {
                    throw new NotFoundException("Không tìm thấy cuộc trò chuyện");
                }

                string imageUrl = null;
                if (dto.Image != null)
                {
                    imageUrl = await SaveImage(dto.Image);
                }

                var sender = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == senderId);

                var message = new Message
                {
                    ConversationId = dto.ConversationId,
                    SenderId = senderId,
                    MessageContent = dto.MessageContent,
                    ImageUrl = imageUrl,
                    SendTime = DateTime.UtcNow,
                    IsRead = false,
                    IsDeleted = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Cập nhật conversation
                conversation.LastMessageId = message.MessageId;
                conversation.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return new MessageResponseDto
                {
                    MessageId = message.MessageId,
                    SenderId = message.SenderId,
                    MessageContent = message.MessageContent,
                    ImageUrl = message.ImageUrl,
                    SendTime = message.SendTime,
                    IsRead = message.IsRead,
                    SenderName = sender?.FullName ?? "Unknown",
                    SenderAvatar = sender?.ProfileImageUrl
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError($"Error in SendMessage: {ex.Message}");
                throw;
            }
        }

        public async Task<ChatHistoryResponseDto> GetChatHistory(int currentUserId, int conversationId)
        {
            var conversation = await _context.Conversations
                .Include(c => c.Seller)
                .Include(c => c.Buyer)
                .Include(c => c.LastMessage)
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId && c.IsActive);

            if (conversation == null)
            {
                throw new NotFoundException("Không tìm thấy cuộc trò chuyện");
            }

            if (currentUserId != conversation.SellerId && currentUserId != conversation.BuyerId)
            {
                throw new UnauthorizedException("Bạn không có quyền xem cuộc trò chuyện này");
            }

            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .OrderBy(m => m.SendTime)
                .ToListAsync();

            // Đánh dấu tin nhắn đã đọc
            await MarkMessagesAsRead(conversationId, currentUserId);

            return new ChatHistoryResponseDto
            {
                Conversation = MapToConversationResponse(conversation),
                Messages = messages.Select(MapToMessageResponse).ToList()
            };
        }

        public async Task MarkMessagesAsRead(int conversationId, int userId)
        {
            var unreadMessages = await _context.Messages
                .Where(m =>
                    m.ConversationId == conversationId &&
                    m.SenderId != userId &&
                    !m.IsRead &&
                    !m.IsDeleted)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        private ConversationResponseDto MapToConversationResponse(Conversation conversation)
        {
            if (conversation == null) throw new ArgumentNullException(nameof(conversation));

            return new ConversationResponseDto
            {
                ConversationId = conversation.ConversationId,
                Seller = new UserInfoDto
                {
                    UserId = conversation.SellerId,
                    FullName = conversation.Seller?.FullName ?? "Unknown",
                    Avatar = conversation.Seller?.ProfileImageUrl
                },
                Buyer = conversation.BuyerId.HasValue && conversation.Buyer != null
                    ? new UserInfoDto
                    {
                        UserId = conversation.BuyerId.Value,
                        FullName = conversation.Buyer.FullName,
                        Avatar = conversation.Buyer.ProfileImageUrl
                    }
                    : null,
                UpdatedAt = conversation.UpdatedAt,
                LastMessage = conversation.LastMessage != null
                    ? MapToMessageResponse(conversation.LastMessage)
                    : null
            };
        }

        private MessageResponseDto MapToMessageResponse(Message message)
        {
            return new MessageResponseDto
            {
                MessageId = message.MessageId,
                SenderId = message.SenderId,
                MessageContent = message.MessageContent,
                ImageUrl = message.ImageUrl,
                SendTime = message.SendTime,
                IsRead = message.IsRead,
                SenderName = message.Sender?.FullName ?? "Unknown",
                SenderAvatar = message.Sender?.ProfileImageUrl
            };
        }
        public async Task<List<ConversationResponseDto>> GetUserConversations(int userId)
        {
            var conversations = await _context.Conversations
                .Include(c => c.Seller)
                .Include(c => c.Buyer)
                .Include(c => c.LastMessage)
                .Where(c => (c.SellerId == userId || c.BuyerId == userId) && c.IsActive)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            return conversations.Select(MapToConversationResponse).ToList();
        }
        public async Task<int> GetUnreadMessageCount(int userId)
        {
            return await _context.Messages
                .Include(m => m.Conversation)
                .Where(m =>
                    (m.Conversation.BuyerId == userId || m.Conversation.SellerId == userId) && // Tin nhắn trong các cuộc hội thoại của user
                    m.SenderId != userId && // Không phải tin nhắn do user gửi
                    !m.IsRead && // Chưa đọc
                    m.Conversation.IsActive // Cuộc hội thoại còn active
                )
                .CountAsync();
        }


        public class BadRequestException : Exception
        {
            public BadRequestException()
            {
            }

            public BadRequestException(string message)
                : base(message)
            {
            }

            public BadRequestException(string message, Exception inner)
                : base(message, inner)
            {
            }
        }
    }
}