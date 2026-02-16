using System.Security.Claims;
using DT_I_Onboarding_Portal.Core.Models;
using DT_I_Onboarding_Portal.Core.Models.Dto;
using DT_I_Onboarding_Portal.Data;
using DT_I_Onboarding_Portal.Data.Stores;
using DT_I_Onboarding_Portal.Services;
using DT_I_Onboarding_Portal.Services.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DT_I_Onboarding_Portal.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly EfUserStore _userStore;
        private readonly TokenService _tokenService;
        private readonly ApplicationDbContext _db;
        private readonly IPasswordHasher<AppUser> _passwordHasher;
        private readonly IOptions<SmtpOptions> _smtpOptions;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            EfUserStore userStore,
            TokenService tokenService,
            ApplicationDbContext db,
            IPasswordHasher<AppUser> passwordHasher,
            IOptions<SmtpOptions> smtpOptions,
            ILogger<AuthController> logger)
        {
            _userStore = userStore;
            _tokenService = tokenService;
            _db = db;
            _passwordHasher = passwordHasher;
            _smtpOptions = smtpOptions;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            var user = await _userStore.ValidateCredentialsAsync(req.Username, req.Password);
            if (user == null) return Unauthorized(new { message = "Invalid credentials" });

            var roles = _userStore.GetRoles(user);
            var (token, expires) = _tokenService.CreateAccessToken(user, roles);

            var resp = new LoginResponse
            {
                Token = token,
                Expires = expires,
                Roles = roles
            };

            return Ok(resp);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetAvailableRoles()
        {
            var roles = await _db.Roles
                .Select(r => new RoleDto { Id = r.Id, Name = r.Name })
                .ToListAsync();

            if (roles.Count == 0)
                return NotFound(new { message = "No roles available." });

            return Ok(roles);
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupRequest req)
        {
            if (!ModelState.IsValid)
                return ValidationProblem(ModelState);

            // Validate role exists
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == req.RoleId);
            if (role == null)
                return BadRequest(new { message = "Invalid role selected." });

            // Check if username already exists
            var existingUser = await _db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (existingUser != null)
                return BadRequest(new { message = "Username already exists." });

            // Check if email already exists
            var existingEmail = await _db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
            if (existingEmail != null)
                return BadRequest(new { message = "Email already registered." });

            // Create new user
            var newUser = new AppUser
            {
                Username = req.Username.Trim(),
                Email = req.Email.Trim().ToLowerInvariant(),
                IsActive = true,
                PasswordHash = _passwordHasher.HashPassword(null!, req.Password)
            };

            _db.Users.Add(newUser);
            await _db.SaveChangesAsync();

            // Assign selected role
            _db.UserRoles.Add(new AppUserRole
            {
                UserId = newUser.Id,
                RoleId = req.RoleId
            });
            await _db.SaveChangesAsync();

            var response = new SignupResponse
            {
                UserId = newUser.Id,
                Username = newUser.Username,
                Email = newUser.Email,
                Message = "User registered successfully. You can now login."
            };

            return CreatedAtAction(nameof(GetProfile), new { id = newUser.Id }, response);
        }

        [HttpGet("whoami")]
        [Authorize]
        public async Task<IActionResult> WhoAmI()
        {
            var username = User.Identity?.Name;

            var roles = User.Claims
                .Where(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles")
                .Select(c => c.Value)
                .Distinct()
                .ToArray();

            return Ok(new { username, roles });
        }

        [HttpGet("profile/{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetProfile([FromRoute] int id)
        {
            var user = await _db.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
                return NotFound(new { message = "User not found." });

            var roles = user.UserRoles?
                .Select(ur => ur.Role?.Name ?? string.Empty)
                .Where(name => !string.IsNullOrEmpty(name))
                .ToArray() ?? Array.Empty<string>();

            return Ok(new
            {
                user.Id,
                user.Username,
                user.Email,
                user.IsActive,
                Roles = roles
            });
        }

        /// <summary>
        /// Diagnostic endpoint to test SMTP connectivity
        /// Development/Testing only - can be disabled in production
        /// </summary>
        [HttpGet("diagnostics/smtp")]
        [AllowAnonymous]
        public async Task<IActionResult> DiagnosticSmtp()
        {
            if (!HttpContext.Request.Host.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) &&
                !HttpContext.Request.Host.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("SMTP diagnostic endpoint accessed from non-localhost: {Host}", 
                    HttpContext.Request.Host.Host);
                return Forbid("SMTP diagnostics only available from localhost");
            }

            try
            {
                var diagnostics = new SmtpConnectionDiagnostics(
                    _logger as ILogger<SmtpConnectionDiagnostics> ?? 
                    LoggerFactory.Create(b => b.AddConsole()).CreateLogger<SmtpConnectionDiagnostics>());
                
                var suite = await diagnostics.RunFullDiagnosticsAsync(_smtpOptions.Value);

                return Ok(new
                {
                    healthy = suite.IsHealthy,
                    summary = suite.GetSummary(),
                    details = new
                    {
                        tcp = new
                        {
                            connected = suite.TcpResult.TcpConnected,
                            message = suite.TcpResult.Message
                        },
                        smtp = new
                        {
                            connected = suite.SmtpResult.SmtpConnected,
                            message = suite.SmtpResult.Message
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running SMTP diagnostics");
                return StatusCode(500, new
                {
                    error = "Diagnostic failed",
                    message = ex.Message
                });
            }
        }
    }
}