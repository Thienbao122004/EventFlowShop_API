using WebAPI_FlowerShopSWP.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;

        public ConversationController(FlowerEventShopsContext context)
        {
            _context = context;
        }

        // GET: api/Conversation
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Conversation>>> GetConversations()
        {
            return await _context.Conversations.ToListAsync();
        }

        // GET: api/Conversation/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Conversation>> GetConversation(int id)
        {
            var conversation = await _context.Conversations.FindAsync(id);

            if (conversation == null)
            {
                return NotFound();
            }

            return conversation;
        }

        // POST: api/Conversation
        [HttpPost]
        public async Task<ActionResult<Conversation>> PostConversation()
        {
            var conversation = new Conversation
            {
                UpdatedAt = DateTime.UtcNow
            };

            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetConversation", new { id = conversation.ConversationId }, conversation);
        }

        // GET: api/Conversation/User/5
        [HttpGet("User/{userId}")]
        public async Task<ActionResult<IEnumerable<Conversation>>> GetUserConversations(int userId)
        {
            var userConversations = await _context.Messages
                .Where(m => m.UserId == userId)
                .Select(m => m.Conversation)
                .Distinct()
                .ToListAsync();

            return userConversations;
        }
    }
}