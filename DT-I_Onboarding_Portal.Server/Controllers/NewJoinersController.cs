using DT_I_Onboarding_Portal.Core.Models;
using DT_I_Onboarding_Portal.Data;
using DT_I_Onboarding_Portal.Core.enums;
using DT_I_Onboarding_Portal.Core.Models.Dto;
using DT_I_Onboarding_Portal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DT_I_Onboarding_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/new-joiners")]
    [Authorize]
    public class NewJoinersController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _email;
        private readonly ILogger<NewJoinersController> _logger;

        public NewJoinersController(ApplicationDbContext db, IEmailSender email, ILogger<NewJoinersController> logger)
        {
            _db = db;
            _email = email;
            _logger = logger;

        }

        /// <summary>Create a new joiner (Admin or User role)</summary>
        [HttpPost]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> CreateNewJoiner([FromBody] CreateNewJoinerDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            if (dto.Email is null)
                return BadRequest(new { message = "Email is required." });

            var normalizedEmail = dto.Email.Trim().ToLowerInvariant();
            var exists = await _db.NewJoiners
                .AnyAsync(nj => nj.Email.ToLower() == normalizedEmail && nj.StartDate.Date == dto.StartDate.Date);
            if (exists)
                return Conflict(new { message = "A new joiner with this email and start date already exists." });

            var newJoiner = new NewJoiner
            {
                FullName = dto.FullName.Trim(),
                Email = normalizedEmail,
                Department = dto.Department?.Trim(),
                ManagerName = dto.ManagerName?.Trim(),
                StartDate = dto.StartDate.Date,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.NewJoiners.Add(newJoiner);
            await _db.SaveChangesAsync();

            var subject = $"Welcome to the team, {newJoiner.FullName}!";
            var html = $@"
        <div style=""font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#333"">
            <h2>Welcome, {newJoiner.FullName} 👋</h2>
            <p>We're excited to have you join the <strong>{newJoiner.Department ?? "team"}</strong> on <strong>{newJoiner.StartDate:MMMM dd, yyyy}</strong>.</p>
            {(string.IsNullOrWhiteSpace(newJoiner.ManagerName) ? "" : $"<p>Your manager will be <strong>{newJoiner.ManagerName}</strong>.</p>")}
            <p>Before your first day, please check your email for onboarding tasks and credentials.</p>
            <hr />
            <p>If you have any questions, reply to this email.</p>
            <p>— Onboarding Team</p>
        </div>";

            var sendResult = await _email.SendEmailAsync(newJoiner.Email, subject, html);

            if (sendResult.Success)
            {
                newJoiner.WelcomeEmailSentAtUtc = DateTime.UtcNow;
                newJoiner.LastSendError = null;
                await _db.SaveChangesAsync();
                return CreatedAtAction(nameof(GetById), new { id = newJoiner.Id }, MapToResponseDto(newJoiner));
            }

            newJoiner.LastSendError = $"{sendResult.ErrorType}: {sendResult.ProviderMessage ?? sendResult.ErrorMessage}";
            await _db.SaveChangesAsync();

            return Accepted(new
            {
                id = newJoiner.Id,
                message = "New joiner created, but failed to send welcome email.",
                errorType = sendResult.ErrorType.ToString(),
                error = sendResult.ErrorMessage,
                providerMessage = sendResult.ProviderMessage
            });

        }

        /// <summary>Get a single new joiner by ID (Admin or User role)</summary>
        [HttpGet("{id:int}")]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var nj = await _db.NewJoiners.FindAsync(id);
            if (nj == null)
                return NotFound(new { message = "New joiner not found." });
            return Ok(MapToResponseDto(nj));
        }

        /// <summary>Get all new joiners with optional filtering</summary>
        [HttpGet]
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] DateTime? startDateFrom, [FromQuery] DateTime? startDateTo)
        {
            var query = _db.NewJoiners.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLowerInvariant();
                query = query.Where(nj => nj.FullName.ToLower().Contains(searchLower) ||
                                          nj.Email.ToLower().Contains(searchLower) ||
                                          nj.Department!.ToLower().Contains(searchLower));
            }

            if (startDateFrom.HasValue)
                query = query.Where(nj => nj.StartDate >= startDateFrom.Value.Date);

            if (startDateTo.HasValue)
                query = query.Where(nj => nj.StartDate <= startDateTo.Value.Date);

            var newJoiners = await query.OrderByDescending(nj => nj.CreatedAtUtc).ToListAsync();
            return Ok(newJoiners.Select(MapToResponseDto).ToList());
        }

        /// <summary>Update a new joiner (Admin only)</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateNewJoiner([FromRoute] int id, [FromBody] UpdateNewJoinerDto dto)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var nj = await _db.NewJoiners.FindAsync(id);
            if (nj == null)
                return NotFound(new { message = "New joiner not found." });

            nj.FullName = dto.FullName.Trim();
            nj.Department = dto.Department?.Trim();
            nj.ManagerName = dto.ManagerName?.Trim();
            nj.StartDate = dto.StartDate.Date;

            await _db.SaveChangesAsync();
            return Ok(new { message = "New joiner updated successfully.", data = MapToResponseDto(nj) });
        }

        /// <summary>Delete a new joiner (Admin only)</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteNewJoiner([FromRoute] int id)
        {
            var nj = await _db.NewJoiners.FindAsync(id);
            if (nj == null)
                return NotFound(new { message = "New joiner not found." });

            _db.NewJoiners.Remove(nj);
            await _db.SaveChangesAsync();
            return Ok(new { message = "New joiner deleted successfully." });
        }

        /// <summary>Resend welcome email (Admin only)</summary>
        [HttpPost("{id:int}/resend-email")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ResendWelcomeEmail([FromRoute] int id)
        {
            var nj = await _db.NewJoiners.FindAsync(id);
            if (nj == null)
                return NotFound(new { message = "New joiner not found." });

            var subject = $"Welcome to the team, {nj.FullName}!";
            var html = $@"<div style=""font-family:Segoe UI,Arial,sans-serif;font-size:14px;color:#333""><h2>Welcome, {nj.FullName} 👋</h2></div>";
            var sendResult = await _email.SendEmailAsync(nj.Email, subject, html);

            if (sendResult.Success)
            {
                nj.WelcomeEmailSentAtUtc = DateTime.UtcNow;
                nj.LastSendError = null;
                await _db.SaveChangesAsync();
                return Ok(new { message = "Welcome email sent successfully.", data = MapToResponseDto(nj) });
            }

            nj.LastSendError = $"{sendResult.ErrorType}: {sendResult.ProviderMessage ?? sendResult.ErrorMessage}";
            await _db.SaveChangesAsync();
            return BadRequest(new { message = "Failed to send welcome email.", errorType = sendResult.ErrorType.ToString(), error = sendResult.ErrorMessage, providerMessage = sendResult.ProviderMessage });
        }

        /// <summary>Get statistics about new joiners (Admin only)</summary>
        [HttpGet("stats/summary")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetStatistics()
        {
            var totalCount = await _db.NewJoiners.CountAsync();
            var emailSentCount = await _db.NewJoiners.CountAsync(nj => nj.WelcomeEmailSentAtUtc != null);
            var emailFailedCount = await _db.NewJoiners.CountAsync(nj => nj.LastSendError != null);
            var upcomingCount = await _db.NewJoiners.CountAsync(nj => nj.StartDate > DateTime.UtcNow.Date);
            return Ok(new { totalNewJoiners = totalCount, emailSent = emailSentCount, emailFailed = emailFailedCount, upcomingJoiners = upcomingCount });
        }

        private static NewJoinerResponseDto MapToResponseDto(NewJoiner nj) => new()
        {
            Id = nj.Id,
            FullName = nj.FullName,
            Email = nj.Email,
            Department = nj.Department,
            ManagerName = nj.ManagerName,
            StartDate = nj.StartDate,
            CreatedAtUtc = nj.CreatedAtUtc,
            WelcomeEmailSentAtUtc = nj.WelcomeEmailSentAtUtc,
            LastSendError = nj.LastSendError
        };
    }
}