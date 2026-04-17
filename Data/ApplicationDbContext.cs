namespace ChatApp.Data
{
	using ChatApp.Models;
	using Microsoft.EntityFrameworkCore;

	public class ApplicationDbContext : DbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options) { }

		public DbSet<User> Users { get; set; }
		public DbSet<Chat> Chats { get; set; }
		public DbSet<Message> Messages { get; set; }
		public DbSet<ChatParticipant> ChatParticipants { get; set; }
		public DbSet<Transaction> Transactions { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Many-to-Many for Chat <-> User via ChatParticipant
			modelBuilder.Entity<ChatParticipant>()
				.HasKey(cp => new { cp.ChatId, cp.UserId });

			modelBuilder.Entity<ChatParticipant>()
				.HasOne(cp => cp.Chat)
				.WithMany(c => c.Participants)
				.HasForeignKey(cp => cp.ChatId);

			modelBuilder.Entity<ChatParticipant>()
				.HasOne(cp => cp.User)
				.WithMany()
				.HasForeignKey(cp => cp.UserId);

			// Transaction relationships
			modelBuilder.Entity<Transaction>()
				.HasOne(t => t.Sender)
				.WithMany()
				.HasForeignKey(t => t.SenderId)
				.OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<Transaction>()
				.HasOne(t => t.Receiver)
				.WithMany()
				.HasForeignKey(t => t.ReceiverId)
				.IsRequired(false)
				.OnDelete(DeleteBehavior.SetNull);
		}
	}

}
