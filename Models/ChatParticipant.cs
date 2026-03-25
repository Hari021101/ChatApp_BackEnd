using System;

namespace ChatApp.Models
{
    public class ChatParticipant
    {
        public Guid ChatId { get; set; }
        public string UserId { get; set; } = string.Empty; // Firebase UID
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public virtual Chat? Chat { get; set; }
        public virtual User? User { get; set; }
    }
}
