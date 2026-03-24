 namespace ChatApp.Models
{
	public class Message
	{
		public Guid Id { get; set; }
		public Guid ConversationId { get; set; }
		public Guid SenderId { get; set; }
		public string Text { get; set; } = string.Empty;
		public string Type { get; set; } = "text"; // text, image, voice
		public DateTime Timestamp { get; set; } = DateTime.UtcNow;
		public string Status { get; set; } = "sent"; // sent, delivered, read
	}
}
