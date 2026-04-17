using ChatApp.Data;
using ChatApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ChatApp.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PaymentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────────────────────
        // GET /api/payments/balance
        // Returns the current user's running balance (sum of receives - sum of sends for Completed tx)
        // ─────────────────────────────────────────────────────────────
        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Sum all completed received amounts (in cents)
            var totalReceived = await _context.Transactions
                .Where(t => t.ReceiverId == userId && t.Status == "Completed")
                .SumAsync(t => t.AmountCents);

            // Sum all completed sent amounts (in cents)
            var totalSent = await _context.Transactions
                .Where(t => t.SenderId == userId && t.Status == "Completed" && t.Type == "send")
                .SumAsync(t => t.AmountCents);

            var balanceCents = totalReceived - totalSent;

            return Ok(new
            {
                balanceCents,
                // Formatted dollar string like "1,240.50"
                balanceFormatted = $"{balanceCents / 100.0:N2}"
            });
        }

        // ─────────────────────────────────────────────────────────────
        // GET /api/payments/transactions?limit=20
        // Returns paginated transaction history for the current user
        // ─────────────────────────────────────────────────────────────
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactions([FromQuery] int limit = 20, [FromQuery] int skip = 0)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var transactions = await _context.Transactions
                .Where(t => t.SenderId == userId || t.ReceiverId == userId)
                .OrderByDescending(t => t.Timestamp)
                .Skip(skip)
                .Take(limit)
                .Select(t => new
                {
                    id          = t.Id,
                    title       = t.Title,
                    subtitle    = t.Subtitle,
                    // Return formatted dollar amount e.g. "$42.50"
                    amount      = $"${t.AmountCents / 100.0:F2}",
                    amountCents = t.AmountCents,
                    // Determine type from the perspective of the current user
                    type   = t.SenderId == userId ? "send" : "receive",
                    status = t.Status,
                    // Format timestamp like "Today, 8:30 PM" on the client; send ISO here
                    timestamp = t.Timestamp
                })
                .ToListAsync();

            return Ok(transactions);
        }

        // ─────────────────────────────────────────────────────────────
        // POST /api/payments/send
        // Initiates a money transfer to another user
        // ─────────────────────────────────────────────────────────────
        [HttpPost("send")]
        public async Task<IActionResult> SendMoney([FromBody] SendMoneyRequest request)
        {
            var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(senderId)) return Unauthorized();

            if (request.AmountCents <= 0)
                return BadRequest("Amount must be greater than zero.");

            if (senderId == request.ReceiverId)
                return BadRequest("You cannot send money to yourself.");

            // Look up the receiver
            var receiver = await _context.Users.FindAsync(request.ReceiverId);
            if (receiver == null)
                return NotFound("Receiver not found.");

            var sender = await _context.Users.FindAsync(senderId);

            // Create a "send" transaction for the sender
            var sendTx = new Transaction
            {
                Id          = Guid.NewGuid(),
                SenderId    = senderId,
                ReceiverId  = request.ReceiverId,
                Title       = receiver.DisplayName,
                Subtitle    = request.Note ?? "Money transfer",
                AmountCents = request.AmountCents,
                Type        = "send",
                Status      = "Completed",
                Timestamp   = DateTime.UtcNow
            };

            // Create a matching "receive" transaction for the receiver
            var receiveTx = new Transaction
            {
                Id          = Guid.NewGuid(),
                SenderId    = senderId,
                ReceiverId  = request.ReceiverId,
                Title       = sender?.DisplayName ?? "Unknown",
                Subtitle    = request.Note ?? "Money received",
                AmountCents = request.AmountCents,
                Type        = "receive",
                Status      = "Completed",
                Timestamp   = DateTime.UtcNow
            };

            _context.Transactions.Add(sendTx);
            _context.Transactions.Add(receiveTx);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Payment sent successfully.",
                transactionId = sendTx.Id
            });
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Request DTOs
    // ─────────────────────────────────────────────────────────────
    public class SendMoneyRequest
    {
        /// <summary>Target user Id</summary>
        public string ReceiverId { get; set; } = string.Empty;

        /// <summary>Amount in cents. E.g. $42.50 → 4250</summary>
        public long AmountCents { get; set; }

        /// <summary>Optional description e.g. "Dinner split"</summary>
        public string? Note { get; set; }
    }
}
