using System;
using System.Collections.Generic;

namespace ChatApp.Models
{
    public class Chat
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public bool IsGroup { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? LastMessage { get; set; }
        public string? ImageURL { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
