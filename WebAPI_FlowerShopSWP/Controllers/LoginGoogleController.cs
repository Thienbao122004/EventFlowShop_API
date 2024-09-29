using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableCors("AllowAll")]
    public class LoginGoogleController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginGoogleController> _logger;

        public LoginGoogleController(FlowerEventShopsContext context, IConfiguration configuration, ILogger<LoginGoogleController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }
        [EnableCors("AllowAll")]
        [HttpGet("login-google")]
        public IActionResult LoginGoogle()
        {
            _logger.LogInformation("LoginGoogle method called");

            var properties = new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleCallback", "LoginGoogle", null, Request.Scheme),
                Items =
        {
            { "LoginProvider", "Google" },
            { ".xsrf", Guid.NewGuid().ToString() }  // Ensure state is unique
        }
            };

            // Start the Google authentication challenge
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }
        [EnableCors("AllowAll")]
        [HttpGet("google-callback")]
        public async Task<IActionResult> GoogleCallback()
        {
            _logger.LogInformation("GoogleCallback method called");
            try
            {
                // Authenticate the user via Google OAuth
                var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

                if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
                {
                    _logger.LogError("Google authentication failed.");
                    return BadRequest("Google authentication failed.");
                }

                // Extract Google user information
                var googleUser = authenticateResult.Principal;
                var email = googleUser.FindFirstValue(ClaimTypes.Email);
                var name = googleUser.FindFirstValue(ClaimTypes.Name);

                // Check if the user exists in the database
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    // Add a new user if not found
                    user = new User
                    {
                        Email = email,
                        Name = name,
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // Generate JWT token
                var tokenString = GenerateJwtToken(user);

                // Redirect back to the frontend with the JWT token in the URL
                var redirectUrl = $"http://localhost:5173/login?token={tokenString}";
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during Google authentication callback");
                return StatusCode(500, "An error occurred during authentication");
            }
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok("Test endpoint working");
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok("Logged out successfully");
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
