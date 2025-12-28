using CarBookingSystem.Domain;
using CarBookingSystem.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext context, IConfiguration config, ILogger<AuthController> logger)
    {
        _context = context;
        _config = config;
        _logger = logger;
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto login)
    {
        try
        {
            _logger.LogInformation($"Login attempt for email: {login.Email}");

            if (string.IsNullOrWhiteSpace(login.Email) || string.IsNullOrWhiteSpace(login.Password))
                return BadRequest(new { message = "Email and password are required" });

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == login.Email);

            if (user == null)
            {
                _logger.LogWarning($"User not found: {login.Email}");
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Simple password comparison (in production, use hashing)
            if (user.PasswordHash != login.Password)
            {
                _logger.LogWarning($"Invalid password for user: {login.Email}");
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (user.Status != "Active")
                return Unauthorized(new { message = "Account is not active" });

            var token = GenerateJwtToken(user);

            _logger.LogInformation($"User {user.Email} logged in successfully. User ID: {user.Id}");

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role,
                    user.Phone
                },
                userId = user.Id  // Make sure this is included
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] User user)
    {
        try
        {
            if (await _context.Users.AnyAsync(u => u.Email == user.Email))
                return BadRequest(new { message = "User already exists" });

            // Set default values
            user.Status = string.IsNullOrWhiteSpace(user.Status) ? "Active" : user.Status;
            user.LicenseNumber = string.IsNullOrWhiteSpace(user.LicenseNumber) ? null : user.LicenseNumber;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User registered: {user.Email} with ID: {user.Id}");

            return Ok(new
            {
                message = "User registered successfully",
                userId = user.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _config.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"] ?? "YourSecretKeyHere12345678901234567890";
        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim("userId", user.Id.ToString()),  // Custom claim for userId
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())  // Standard claim
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"] ?? "CarBookingAPI",
            audience: jwtSettings["Audience"] ?? "CarBookingClient",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(jwtSettings["ExpiryMinutes"] ?? "60")),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}