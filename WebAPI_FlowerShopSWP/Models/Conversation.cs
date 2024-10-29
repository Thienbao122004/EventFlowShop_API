using System;
using System.Collections.Generic;

namespace WebAPI_FlowerShopSWP.Models;

public class Conversation
{
    public int ConversationId { get; set; }
    public int SellerId { get; set; }
    public int? BuyerId { get; set; }
    public int? LastMessageId { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; }

    public virtual User Seller { get; set; }
    public virtual User Buyer { get; set; }
    public virtual Message LastMessage { get; set; }
    public virtual ICollection<Message> Messages { get; set; }
}