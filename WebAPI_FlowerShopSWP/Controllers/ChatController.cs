using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(FlowerEventShopsContext context, IHubContext<ChatHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        [HttpPost("send")]
        [Authorize]
        public async Task<IActionResult> SendMessage([FromBody] MessageDto messageDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var message = new Message
                {
                    UserId = messageDto.UserId,
                    ConversationId = messageDto.ConversationId,
                    MessageContent = messageDto.MessageContent,
                    SendTime = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                await _hubContext.Clients.Group(message.ConversationId.ToString())
                    .SendAsync("ReceiveMessage", message.UserId, message.MessageContent, message.ConversationId);

                return CreatedAtAction(nameof(GetChatHistory), new { conversationId = message.ConversationId }, message);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "Có lỗi xảy ra khi gửi tin nhắn");
            }
        }

        [HttpPost("create")]
        [Authorize]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationDto dto)
        {
            var conversation = new Conversation
            {
                UserId = dto.UserId,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetConversationParticipants), new { id = conversation.ConversationId }, conversation);
        }

        public class CreateConversationDto
        {
            public int UserId { get; set; }
        }
        [HttpGet("participants/{conversationId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetConversationParticipants(int conversationId)
        {
            var participants = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .Select(m => m.User)
                .Distinct()
                .Select(u => new { u.UserId, u.Name })
                .ToListAsync();

            if (!participants.Any())
            {
                return NotFound("Không tìm thấy người tham gia trong cuộc trò chuyện này.");
            }

            return Ok(participants);
        }

        [HttpGet("history/{conversationId}")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetChatHistory(int conversationId)
        {
            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .Include(m => m.User)
                .OrderBy(m => m.SendTime)
                .Select(m => new
                {
                    m.MessageId,
                    m.ConversationId,
                    m.UserId,
                    m.MessageContent,
                    m.SendTime,
                    m.IsRead,
                    UserName = m.User.Name
                })
                .ToListAsync();

            if (!messages.Any())
            {
                return Ok(new { message = "Cuộc trò chuyện chưa có tin nhắn nào." });
            }

            return Ok(messages);
        }

        public class MessageDto
        {
            public int UserId { get; set; }
            public int ConversationId { get; set; }
            public string MessageContent { get; set; }
        }
    }
}