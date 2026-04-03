using ChatApp.Data;
using ChatApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ChatsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Get all chats for the current user (based on their Firebase UID)
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserChats(string userId)
        {
            var chats = await _context.ChatParticipants
                .Where(cp => cp.UserId == userId)
                .Include(cp => cp.Chat)
                .ThenInclude(c => c.Participants)
                .ThenInclude(p => p.User)
                .Select(cp => new
                {
                    Id = cp.Chat.Id,
                    Title = cp.Chat.Title,
                    IsGroup = cp.Chat.IsGroup,
                    ImageURL = cp.Chat.ImageURL,
                    LastMessage = cp.Chat.LastMessage,
                    UpdatedAt = cp.Chat.UpdatedAt,
                    UnreadCount = _context.Messages.Count(m => m.ChatId == cp.Chat.Id && !m.IsRead && m.SenderId != userId),
                    Participants = cp.Chat.Participants.Select(p => new
                    {
                        UserId = p.UserId,
                        DisplayName = p.User != null ? p.User.DisplayName : "Unknown",
                        PhotoURL = p.User != null ? p.User.PhotoURL : null
                    })
                })
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();

            return Ok(chats);
        }

        // Get message history for a specific chat
        [HttpGet("{chatId}/messages")]
        public async Task<ActionResult<IEnumerable<Message>>> GetChatMessages(Guid chatId, [FromQuery] int limit = 50, [FromQuery] DateTime? before = null)
        {
            var query = _context.Messages
                .Where(m => m.ChatId == chatId);

            if (before.HasValue)
            {
                query = query.Where(m => m.Timestamp < before.Value);
            }

            var messages = await query
                .OrderByDescending(m => m.Timestamp)
                .Take(limit)
                .ToListAsync();

            return Ok(messages.OrderBy(m => m.Timestamp)); // Return in chronological order
        }

        [HttpPost("direct")]
        public async Task<ActionResult<Chat>> CreateDirectChat([FromBody] DirectChatRequest request)
        {
            // Check if a direct chat already exists
            var existingChat = await _context.Chats
                .Where(c => !c.IsGroup)
                .Where(c => c.Participants.Any(p => p.UserId == request.UserId1) && 
                            c.Participants.Any(p => p.UserId == request.UserId2))
                .FirstOrDefaultAsync();

            if (existingChat != null)
            {
                return Ok(existingChat);
            }

            var chat = new Chat
            {
                Id = Guid.NewGuid(),
                IsGroup = false,
                Title = "Direct Chat",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            chat.Participants.Add(new ChatParticipant { ChatId = chat.Id, UserId = request.UserId1 });
            chat.Participants.Add(new ChatParticipant { ChatId = chat.Id, UserId = request.UserId2 });

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetChatMessages), new { chatId = chat.Id }, chat);
        }

        // Create a new group chat
        [HttpPost("group")]
        public async Task<ActionResult<Chat>> CreateGroupChat([FromBody] GroupChatRequest request)
        {
            var chat = new Chat
            {
                Id = Guid.NewGuid(),
                IsGroup = true,
                Title = request.Title,
                ImageURL = request.ImageURL,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastMessage = "Group created"
            };

            foreach (var userId in request.ParticipantIds)
            {
                chat.Participants.Add(new ChatParticipant { ChatId = chat.Id, UserId = userId });
            }

            _context.Chats.Add(chat);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetChatMessages), new { chatId = chat.Id }, chat);
        }
    }

    public class DirectChatRequest
    {
        public string UserId1 { get; set; } = string.Empty;
        public string UserId2 { get; set; } = string.Empty;
    }

    public class GroupChatRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? ImageURL { get; set; }
        public List<string> ParticipantIds { get; set; } = new List<string>();
    }
}
