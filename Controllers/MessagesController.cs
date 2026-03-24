using ChatApp.Data;
using ChatApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class MessagesController : ControllerBase
	{
		private readonly ApplicationDbContext _context;
		public MessagesController(ApplicationDbContext context) => _context = context;

		[HttpGet("{conversationId}")]
		public async Task<ActionResult<IEnumerable<Message>>> GetMessages(Guid conversationId)
		{
			return await _context.Messages
				.Where(m => m.ConversationId == conversationId)
				.OrderBy(m => m.Timestamp)
				.ToListAsync();
		}
	}
}

