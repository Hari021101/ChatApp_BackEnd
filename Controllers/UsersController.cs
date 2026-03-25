using ChatApp.Data;
using ChatApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting; // Add this for file paths

using Microsoft.AspNetCore.Authorization;

namespace ChatApp.Controllers
{
	[Authorize]
	[ApiController]
	[Route("api/[controller]")]
	public class UsersController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		private readonly IWebHostEnvironment _environment; // Add this

		public UsersController(ApplicationDbContext context, IWebHostEnvironment environment)
		{
			_context = context;
			_environment = environment;
		}

		[HttpGet]
		public async Task<ActionResult<IEnumerable<User>>> GetUsers()
		{
			return await _context.Users.ToListAsync();
		}

		[HttpPost]
		public async Task<ActionResult<User>> CreateUser(User user)
		{
			if (string.IsNullOrEmpty(user.Id))
			{
				user.Id = Guid.NewGuid().ToString();
			}

			_context.Users.Add(user);
			await _context.SaveChangesAsync();
			return CreatedAtAction(nameof(GetUsers), new { id = user.Id }, user);
		}

		// --- ADDED THIS METHOD FOR IMAGE UPLOADS ---
		[HttpPost("upload")]
		public async Task<IActionResult> UploadImage(IFormFile file)
		{
			// 1. Log that we received a request
			Console.WriteLine("DEBUG: Received upload request");

			if (file == null || file.Length == 0)
			{
				Console.WriteLine("DEBUG: File is null or empty");
				return BadRequest("No file uploaded.");
			}

			try
			{
				var rootPath = _environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
				Console.WriteLine($"DEBUG: Root Path is {rootPath}");

				var uploadsFolder = Path.Combine(rootPath, "uploads");
				if (!Directory.Exists(uploadsFolder))
				{
					Console.WriteLine("DEBUG: Creating uploads folder...");
					Directory.CreateDirectory(uploadsFolder);
				}

				var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
				var filePath = Path.Combine(uploadsFolder, fileName);
				Console.WriteLine($"DEBUG: Saving to {filePath}");

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await file.CopyToAsync(stream);
				}

				Console.WriteLine("DEBUG: Upload Success!");
				return Ok(new { url = $"/uploads/{fileName}" });
			}
			catch (Exception ex)
			{
				// --- THIS WILL PRINT THE ACTUAL ERROR TO THE CONSOLE ---
				Console.WriteLine("CRITICAL ERROR DURING UPLOAD:");
				Console.WriteLine(ex.ToString());
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("profile/{email}")]
		public async Task<ActionResult<User>> GetProfile(string email)
		{
			var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
			if (user == null) return NotFound();
			return user;
		}

		[HttpGet("search")]
		public async Task<ActionResult<IEnumerable<User>>> Search([FromQuery] string query)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				return Ok(new List<User>());
			}

			var lowerQuery = query.ToLower();
			var users = await _context.Users
				.Where(u => u.DisplayName.ToLower().Contains(lowerQuery) || 
                            u.Email.ToLower().Contains(lowerQuery))
				.Take(20) // Limit to top 20 results
				.ToListAsync();

			return Ok(users);
		}

		[HttpPut("profile")]
		public async Task<IActionResult> UpdateProfile([FromBody] User updatedUser)
		{
			Console.WriteLine($"DEBUG: Received profile update request for {updatedUser?.Email}");

			if (updatedUser == null)
			{
				Console.WriteLine("DEBUG: UpdateProfile received null user object");
				return BadRequest("Invalid user data");
			}

			try
			{
				var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == updatedUser.Email);
				if (user == null)
				{
					Console.WriteLine($"DEBUG: User not found for email {updatedUser.Email}");
					return NotFound();
				}

				Console.WriteLine($"DEBUG: Updating user {user.Id}");

				// Sync fields from the request
				user.DisplayName = updatedUser.DisplayName;
				user.PhotoURL = updatedUser.PhotoURL;
				user.Bio = updatedUser.Bio;
				user.Location = updatedUser.Location;
				user.DateOfBirth = updatedUser.DateOfBirth;
				user.PhoneNumber = updatedUser.PhoneNumber;
				user.Website = updatedUser.Website;

				await _context.SaveChangesAsync();
				Console.WriteLine("DEBUG: Profile update saved successfully!");
				return NoContent();
			}
			catch (Exception ex)
			{
				Console.WriteLine("CRITICAL ERROR DURING PROFILE UPDATE:");
				Console.WriteLine(ex.ToString());
				return StatusCode(500, ex.Message);
			}
		}

	}
}
