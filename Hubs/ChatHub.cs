namespace ChatApp.Hubs
{
	using ChatApp.Data;
	using ChatApp.Models;
	using Microsoft.EntityFrameworkCore;
	using Microsoft.AspNetCore.SignalR;

	public class ChatHub : Hub
	{
		private readonly ApplicationDbContext _context;

		public ChatHub(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task JoinChat(string chatId)
		{
			await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
		}

		public async Task SendMessage(string chatId, string senderId, string content, string messageType = "text")
		{
			var chatGuid = Guid.Parse(chatId);
			
			// 1. Create and Save Message to DB
			var message = new Message
			{
				Id = Guid.NewGuid(),
				ChatId = chatGuid,
				SenderId = senderId,
				Content = content,
				Timestamp = DateTime.UtcNow,
				MessageType = messageType
			};

			_context.Messages.Add(message);

			// 2. Update Chat's LastMessage and UpdatedAt
			var chat = await _context.Chats.FindAsync(chatGuid);
			if (chat != null)
			{
				chat.LastMessage = content;
				chat.UpdatedAt = DateTime.UtcNow;
			}

			await _context.SaveChangesAsync();

			// 3. Broadcast to എല്ലാവരും in the Chat Group
			await Clients.Group(chatId).SendAsync("ReceiveMessage", chatId, senderId, content, message.Timestamp, messageType);
		}

		public override async Task OnConnectedAsync()
		{
			await base.OnConnectedAsync();
		}
	}
}
