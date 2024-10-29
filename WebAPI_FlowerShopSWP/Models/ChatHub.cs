using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Models
{
    public class ChatHub : Hub
    {
        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, conversationId);
        }
        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationId);
        }
        public async Task MarkMessagesAsRead(string conversationId, List<int> messageIds)
        {
            await Clients.Group(conversationId).SendAsync("MessageRead", messageIds);
        }
    }
}