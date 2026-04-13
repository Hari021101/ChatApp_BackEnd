namespace ChatApp.Hubs
{
	using ChatApp.Data;
	using ChatApp.Models;
	using Microsoft.EntityFrameworkCore;
	using Microsoft.AspNetCore.SignalR;
	using Microsoft.AspNetCore.Authorization;
	using System;
	using System.Threading.Tasks;

	using ChatApp.Services;

	[Authorize]
	public class ChatHub : Hub
	{
		private readonly ApplicationDbContext _context;
		private readonly INotificationService _notificationService;

		public ChatHub(ApplicationDbContext context, INotificationService notificationService)
		{
			_context = context;
			_notificationService = notificationService;
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
				MessageType = messageType,
				IsRead = false
			};

			_context.Messages.Add(message);

			// 2. Update Chat's LastMessage and UpdatedAt
			var chat = await _context.Chats
				.Include(c => c.Participants)
				.ThenInclude(p => p.User)
				.FirstOrDefaultAsync(c => c.Id == chatGuid);

			if (chat != null)
			{
				chat.LastMessage = content;
				chat.UpdatedAt = DateTime.UtcNow;

				// 2.5 Send Push Notifications to offline users
				var sender = await _context.Users.FindAsync(senderId);
				var senderName = sender?.DisplayName ?? "Someone";

				foreach (var participant in chat.Participants)
				{
					if (participant.UserId != senderId && participant.User != null)
					{
						if (!participant.User.IsOnline && !string.IsNullOrEmpty(participant.User.PushToken))
						{
							var title = chat.IsGroup ? $"{chat.Title} ({senderName})" : senderName;
							var notificationContent = messageType == "text" ? content : $"Sent a {messageType}";

							await _notificationService.SendNotificationAsync(
								participant.User.PushToken,
								title,
								notificationContent,
								new { chatId = chatId }
							);
						}
					}
				}
			}

			await _context.SaveChangesAsync();

			// 3. Broadcast to എല്ലാവരും in the Chat Group
			await Clients.Group(chatId).SendAsync("ReceiveMessage", chatId, senderId, content, message.Timestamp, messageType, message.IsRead);
		}

		public async Task MarkAsRead(string chatId)
		{
			var userId = Context.UserIdentifier;
			if (string.IsNullOrEmpty(userId) || !Guid.TryParse(chatId, out var chatGuid)) return;

			// Mark all unread messages in this chat NOT sent by the current user as read
			var unreadMessages = await _context.Messages
				.Where(m => m.ChatId == chatGuid && m.SenderId != userId && !m.IsRead)
				.ToListAsync();

			if (unreadMessages.Any())
			{
				var now = DateTime.UtcNow;
				foreach (var msg in unreadMessages)
				{
					msg.IsRead = true;
					msg.ReadAt = now;
				}

				await _context.SaveChangesAsync();

				// Broadcast that messages were read to എല്ലാവരും in the Chat Group (e.g. the sender)
				await Clients.Group(chatId).SendAsync("MessagesRead", chatId, userId, now);
			}
		}

		public override async Task OnConnectedAsync()
		{
			var userId = Context.UserIdentifier;
			if (!string.IsNullOrEmpty(userId))
			{
				var user = await _context.Users.FindAsync(userId);
				if (user != null)
				{
					user.IsOnline = true;
					await _context.SaveChangesAsync();
					await Clients.All.SendAsync("UserPresenceUpdate", userId, true, DateTime.UtcNow);
				}
			}
			await base.OnConnectedAsync();
		}

		public override async Task OnDisconnectedAsync(Exception? exception)
		{
			var userId = Context.UserIdentifier;
			if (!string.IsNullOrEmpty(userId))
			{
				var user = await _context.Users.FindAsync(userId);
				if (user != null)
				{
					user.IsOnline = false;
					user.LastSeen = DateTime.UtcNow;
					await _context.SaveChangesAsync();
					await Clients.All.SendAsync("UserPresenceUpdate", userId, false, user.LastSeen);
				}
			}
			await base.OnDisconnectedAsync(exception);
		}
	}
}
