using ChatApp.Data;
using ChatApp.Hubs;
using ChatApp.Services;
using Microsoft.EntityFrameworkCore;

try
{
	var builder = WebApplication.CreateBuilder(args);

	builder.Services.AddDbContext<ApplicationDbContext>(options =>
		options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

	builder.Services.AddCors(options =>
	{
		options.AddDefaultPolicy(policy =>
		{
			policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
		});
	});

	builder.Services.AddControllers();
	builder.Services.AddSignalR();
	builder.Services.AddEndpointsApiExplorer();
	builder.Services.AddSwaggerGen();
	builder.Services.AddHttpClient<INotificationService, ExpoNotificationService>();
	builder.Services.AddScoped<IAuthService, AuthService>();

	// --- ADD JWT AUTHENTICATION ---
	builder.Services.AddAuthentication("Bearer")
		.AddJwtBearer("Bearer", options =>
		{
			options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
			{
				ValidateIssuerSigningKey = true,
				IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
					System.Text.Encoding.UTF8.GetBytes(builder.Configuration.GetSection("Jwt:Key").Value ?? "DEFAULT_SECRET")),
				ValidateIssuer = false, // Set to true if you want to validate the issuer string
				ValidateAudience = false,
				ValidateLifetime = true,
				ClockSkew = TimeSpan.Zero
			};
		});
	// ------------------------------

	var app = builder.Build();

	// 1. Database Creation
	using (var scope = app.Services.CreateScope())
	{
		var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
		db.Database.EnsureCreated();
	}

	if (app.Environment.IsDevelopment())
	{
		app.UseSwagger();
		app.UseSwaggerUI();
	}

	app.UseStaticFiles();
	app.UseRouting();
	app.UseCors();
	// app.UseHttpsRedirection(); // Commented out to avoid local SSL issues

	app.UseAuthentication(); // --- ADD THIS ---
	app.UseAuthorization();

	app.MapControllers();
	app.MapHub<ChatHub>("/chathub");

	Console.WriteLine("Server is starting...");
	app.Run();
}
catch (Exception ex)
{
	// --- THIS WILL CATCH THE CRASH ON STARTUP ---
	Console.WriteLine("SERVER CRASHED ON STARTUP:");
	Console.WriteLine(ex.ToString());
	Console.WriteLine("Press any key to exit...");
	Console.ReadKey();
}