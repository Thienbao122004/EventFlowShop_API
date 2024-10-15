using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Models
{
    public class ChatHub : Hub
    {
        private readonly FlowerEventShopsContext _context;

        public ChatHub(FlowerEventShopsContext context)
        {
            _context = context;
        }

        public async Task JoinConversation(string conversationId)
        {
            Console.WriteLine($"Attempting to join conversation: {conversationId}");
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
            Console.WriteLine($"Successfully joined conversation: {conversationId}");
        }

        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task SendMessage(int userId, string messageContent, int conversationId)
        {
            try
            {
                var conversation = await _context.Conversations.FindAsync(conversationId);
                if (conversation == null)
                {
                    conversation = new Conversation
                    {
                        UserId = userId,  // Sử dụng UserId khi tạo conversation mới
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Conversations.Add(conversation);
                    await _context.SaveChangesAsync();
                    conversationId = conversation.ConversationId;
                }
                else
                {
                    conversation.UpdatedAt = DateTime.UtcNow;
                    _context.Conversations.Update(conversation);
                }


                var message = new Message
                {
                    UserId = userId,
                    MessageContent = messageContent,
                    ConversationId = conversationId,
                    SendTime = DateTime.UtcNow,
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId);
                var userName = user?.Name ?? "Unknown User";

                await Clients.Group(conversationId.ToString()).SendAsync("ReceiveMessage", userId, userName, messageContent, conversationId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error in SendMessage: {ex.Message}");
                Console.Error.WriteLine($"Stack Trace: {ex.StackTrace}");
                await Clients.Caller.SendAsync("SendMessageError", $"Có lỗi xảy ra khi gửi tin nhắn: {ex.Message}");
            }
        }
    }
}