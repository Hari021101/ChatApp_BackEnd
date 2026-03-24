namespace ChatApp.Data
{
	using ChatApp.Models;
	using Microsoft.EntityFrameworkCore;

	public class ApplicationDbContext : DbContext
	{
		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
			: base(options) { }

		public DbSet<User> Users { get; set; }
		public DbSet<Message> Messages { get; set; }
	}

}
