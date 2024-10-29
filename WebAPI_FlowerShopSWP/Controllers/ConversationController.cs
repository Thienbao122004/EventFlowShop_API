using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebAPI_FlowerShopSWP.Models;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
public class ConversationController : ControllerBase
{
    private readonly FlowerEventShopsContext _context;

    public ConversationController(FlowerEventShopsContext context)
    {
        _context = context;
    }

    // POST: api/Conversation/messages/{conversationId}
    [HttpPost("messages/{conversationId}")]
    public async Task<ActionResult<object>> SendMessage(int conversationId, [FromBody] SendMessageRequest request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.ConversationId == conversationId);

            if (conversation == null)
            {
                return NotFound("Không tìm thấy cuộc trò chuyện.");
            }

            if (conversation.SellerId != userId && conversation.BuyerId != userId)
            {
                return Forbid("Bạn không có quyền gửi tin nhắn trong cuộc trò chuyện này.");
            }

            // Tạo tin nhắn mới
            var message = new Message
            {
                ConversationId = conversationId,
                SenderId = userId,
                MessageContent = request.Content,
                SendTime = DateTime.UtcNow,
                IsRead = false,
                IsDeleted = false
            };

            // Thêm tin nhắn vào database
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // Cập nhật LastMessageId và UpdatedAt của conversation
            conversation.LastMessageId = message.MessageId;
            conversation.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            // Trả về thông tin tin nhắn
            return Ok(new
            {
                message.MessageId,
                message.ConversationId,
                message.SenderId,
                SenderName = await _context.Users
                    .Where(u => u.UserId == userId)
                    .Select(u => u.Name)
                    .FirstOrDefaultAsync(),
                message.MessageContent,
                message.SendTime,
                message.IsRead
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, "Có lỗi xảy ra khi gửi tin nhắn.");
        }
    }

    // GET: api/Conversation/User/{userId}
    [HttpGet("User/{userId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetUserConversations(int userId)
    {
        var conversations = await _context.Conversations
            .Include(c => c.Seller)
            .Include(c => c.Buyer)
            .Include(c => c.LastMessage)
            .Where(c => (c.SellerId == userId || c.BuyerId == userId) && c.IsActive)
            .OrderByDescending(c => c.UpdatedAt)
            .Select(c => new
            {
                c.ConversationId,
                c.UpdatedAt,
                Seller = new
                {
                    c.Seller.UserId,
                    c.Seller.FullName,
                    c.Seller.ProfileImageUrl
                },
                Buyer = new
                {
                    c.Buyer.UserId,
                    c.Buyer.FullName,
                    c.Buyer.ProfileImageUrl
                },
                LastMessage = c.LastMessage == null ? null : new
                {
                    c.LastMessage.MessageId,
                    c.LastMessage.SenderId,
                    c.LastMessage.MessageContent,
                    c.LastMessage.SendTime,
                    c.LastMessage.IsRead
                }
            })
            .ToListAsync();

        return Ok(conversations);
    }

    // GET: api/Conversation/messages/{conversationId}
    [HttpGet("messages/{conversationId}")]
    public async Task<ActionResult<IEnumerable<object>>> GetMessages(
        int conversationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
            .OrderByDescending(m => m.SendTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(m => new
            {
                m.MessageId,
                m.ConversationId,
                m.SenderId,
                SenderName = m.Sender.Name,
                SenderAvatar = m.Sender.ProfileImageUrl,
                m.MessageContent,
                m.SendTime,
                m.IsRead
            })
            .ToListAsync();

        return Ok(messages);
    }
}

public class SendMessageRequest
{
    public string Content { get; set; }
}