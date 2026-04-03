using System;

namespace ChatApp.Models
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid ChatId { get; set; }
        public string SenderId { get; set; } = string.Empty; // Firebase UID
        public string Content { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string MessageType { get; set; } = "text"; // text, image, audio, video
        public bool IsRead { get; set; } = false;
        public DateTime? ReadAt { get; set; }

        public virtual Chat? Chat { get; set; }
    }
}
