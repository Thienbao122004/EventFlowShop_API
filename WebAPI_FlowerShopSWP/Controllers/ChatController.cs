using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using WebAPI_FlowerShopSWP.DTO;
using WebAPI_FlowerShopSWP.Services;
using WebAPI_FlowerShopSWP.DTO.WebAPI_FlowerShopSWP.Exceptions;
using WebAPI_FlowerShopSWP.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ChatController> _logger; // Thêm logger

        public ChatController(
            IChatService chatService,
            IHubContext<ChatHub> hubContext,
            IWebHostEnvironment environment, ILogger<ChatController> logger) // Thêm parameter

        {
            _chatService = chatService;
            _hubContext = hubContext;
            _environment = environment;
            _logger = logger;
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationDto dto)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                if (currentUserId == dto.SellerId)
                {
                    return BadRequest(new { message = "Không thể tạo cuộc trò chuyện với chính mình" });
                }

                var result = await _chatService.CreateConversation(currentUserId, dto);
                return Ok(result);
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo cuộc trò chuyện" });
            }
        }

        [HttpPost("send")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromForm] SendMessageDto dto)
        {
            try
            {
                _logger.LogInformation($"Receiving message request...");
                var senderId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                // Validate input
                if (dto.ConversationId <= 0)
                {
                    return BadRequest(new { message = "ConversationId không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(dto.MessageContent) && dto.Image == null)
                {
                    return BadRequest(new { message = "Vui lòng nhập tin nhắn hoặc chọn ảnh để gửi" });
                }

                var result = await _chatService.SendMessage(senderId, dto);

                // Gửi tin nhắn qua SignalR với đầy đủ thông tin người gửi
                await _hubContext.Clients.Group(dto.ConversationId.ToString()).SendAsync("ReceiveMessage", new
                {
                    messageId = result.MessageId,
                    conversationId = dto.ConversationId,
                    senderId = senderId,
                    senderName = result.SenderName,
                    senderAvatar = result.SenderAvatar,
                    messageContent = dto.MessageContent,
                    imageUrl = result.ImageUrl,
                    sendTime = DateTime.UtcNow
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in SendMessage: {ex}");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("history/{conversationId}")]
        public async Task<ActionResult<ChatHistoryResponseDto>> GetChatHistory(int conversationId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedException("Không tìm thấy thông tin người dùng"));

                var result = await _chatService.GetChatHistory(currentUserId, conversationId);
                return Ok(result);
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tải lịch sử chat" });
            }
        }

        [HttpGet("conversation-info/{conversationId}")]
        public async Task<ActionResult<ConversationResponseDto>> GetConversationInfo(int conversationId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedException("Không tìm thấy thông tin người dùng"));

                var result = await _chatService.GetChatHistory(currentUserId, conversationId);
                return Ok(result.Conversation);
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tải thông tin cuộc trò chuyện" });
            }
        }

        [HttpPost("mark-as-read/{conversationId}")]
        public async Task<IActionResult> MarkMessagesAsRead(int conversationId)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? throw new UnauthorizedException("Không tìm thấy thông tin người dùng"));

                await _chatService.MarkMessagesAsRead(conversationId, currentUserId);

                await _hubContext.Clients.Group(conversationId.ToString())
                    .SendAsync("MessagesRead", new { conversationId, userId = currentUserId });

                return Ok();
            }
            catch (UnauthorizedException ex)
            {
                return StatusCode(403, new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi đánh dấu tin nhắn đã đọc" });
            }
        }

        [HttpGet("conversations")]
        [Authorize]
        public async Task<ActionResult<List<ConversationResponseDto>>> GetUserConversations()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var conversations = await _chatService.GetUserConversations(currentUserId);
                return Ok(conversations);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tải danh sách chat" });
            }
        }

        [HttpGet("unread-count")]
        [Authorize]
        public async Task<ActionResult<int>> GetUnreadMessageCount()
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var count = await _chatService.GetUnreadMessageCount(currentUserId);
                return Ok(count);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Có lỗi xảy ra khi lấy số tin nhắn chưa đọc" });
            }
        }

        [HttpGet("images/{fileName}")]
        public IActionResult GetImage(string fileName)
        {
            try
            {
                var path = Path.Combine(_environment.WebRootPath, "chat-images", fileName);
                if (!System.IO.File.Exists(path))
                {
                    return NotFound();
                }

                var imageBytes = System.IO.File.ReadAllBytes(path);
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var contentType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    _ => "application/octet-stream"
                };

                return File(imageBytes, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting image {fileName}: {ex.Message}");
                return StatusCode(500, "Error retrieving image");
            }
        }
    }
}