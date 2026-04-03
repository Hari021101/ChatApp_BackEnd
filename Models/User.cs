namespace ChatApp.Models
{
	public class User
	{
			public string Id { get; set; } = string.Empty;
			public string Email { get; set; } = string.Empty;
			public string DisplayName { get; set; } = string.Empty;
			public string? PhotoURL { get; set; }
			public bool IsOnline { get; set; }
			public DateTime LastSeen { get; set; }
			public string? PhoneNumber { get; set; }
			public string? Bio { get; set; }
			public string? Location { get; set; }
			public string? Website { get; set; }
			public string? DateOfBirth { get; set; }
			public string? PushToken { get; set; }
	}
}
