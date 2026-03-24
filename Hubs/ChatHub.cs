namespace ChatApp.Hubs
{
	using Microsoft.AspNetCore.SignalR;

	public class ChatHub : Hub
	{
		public async Task SendMessage(string user, string message)
		{
			// Broadcast to all connected clients
			await Clients.All.SendAsync("ReceiveMessage", user, message);
		}

		public override async Task OnConnectedAsync()
		{
			// Logic for when a user connects (e.g., set IsOnline = true)
			await base.OnConnectedAsync();
		}
	}

}
