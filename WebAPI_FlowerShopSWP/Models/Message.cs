﻿using System;
using System.Collections.Generic;

namespace WebAPI_FlowerShopSWP.Models;

public class Message
{
    public int MessageId { get; set; }
    public int ConversationId { get; set; }
    public int SenderId { get; set; }
    public string? MessageContent { get; set; }
    public string? ImageUrl { get; set; }
    public DateTime SendTime { get; set; }
    public bool IsRead { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public virtual Conversation Conversation { get; set; }
    public virtual User Sender { get; set; }
}
