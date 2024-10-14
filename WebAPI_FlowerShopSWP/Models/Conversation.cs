using System;
using System.Collections.Generic;

namespace WebAPI_FlowerShopSWP.Models;

public partial class Conversation
{
    public int ConversationId { get; set; }
    public int UserId { get; set; }  
    public DateTime? UpdatedAt { get; set; }

    public virtual User User { get; set; }  
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}