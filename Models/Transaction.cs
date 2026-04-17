using System;

namespace ChatApp.Models
{
    public class Transaction
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>The user who initiated the transaction</summary>
        public string SenderId { get; set; } = string.Empty;

        /// <summary>The user who received the transaction</summary>
        public string? ReceiverId { get; set; }

        /// <summary>Display name for the other party in the transaction</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>A short description e.g. "Sent for dinner"</summary>
        public string Subtitle { get; set; } = string.Empty;

        /// <summary>Amount in smallest currency unit (cents). E.g. $42.50 = 4250</summary>
        public long AmountCents { get; set; }

        /// <summary>"send" or "receive"</summary>
        public string Type { get; set; } = "send";

        /// <summary>"Completed", "Pending", or "Failed"</summary>
        public string Status { get; set; } = "Pending";

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual User? Sender { get; set; }
        public virtual User? Receiver { get; set; }
    }
}
