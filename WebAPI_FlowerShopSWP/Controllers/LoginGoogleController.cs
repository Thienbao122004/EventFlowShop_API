using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Oauth2.v2;
using Google.Apis.Oauth2.v2.Data;
using Google.Apis.Services;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        public class GoogleLoginRequest
        {
            public string? AccessToken { get; set; }
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.AccessToken))
                {
                    return BadRequest("Access token is required");
                }
                _logger.LogInformation($"Received Access token: {request.AccessToken}");

                var userInfoClient = new Oauth2Service(new BaseClientService.Initializer
                {
                    HttpClientInitializer = GoogleCredential.FromAccessToken(request.AccessToken)
                });
                var userInfo = await userInfoClient.Userinfo.Get().ExecuteAsync();

                // Find user
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userInfo.Email);

                if (user == null)
                {
                    _logger.LogInformation($"New user detected: {userInfo.Email}");
                    return Ok(new
                    {
                        IsNewUser = true,
                        Email = userInfo.Email,
                        Name = userInfo.Name
                    });
                }

                _logger.LogInformation($"Existing user found: {user.Email}, UserId: {user.UserId}");
                var token = GenerateJwtToken(user);
                return Ok(new
                {
                    IsNewUser = false,
                    Token = token,
                    User = new { user.UserId, user.Name, user.Email, user.UserType }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during Google authentication");
                return StatusCode(500, "An error occurred during authentication: {ex.Message}");
            }
        }

        [HttpPost("complete-registration")]
        public async Task<IActionResult> CompleteRegistration([FromBody] CompleteRegistrationRequest request)
        {
            try
            {
                _logger.LogInformation($"Completing registration for: {request.Email}");

                // Tìm UserId lớn nhất hiện tại
                var maxUserId = await _context.Users.MaxAsync(u => (int?)u.UserId) ?? 0;
                var newUserId = maxUserId + 1;

                var user = new User
                {
                    UserId = newUserId, // Gán UserId mới
                    Email = request.Email,
                    FullName = request.FullName,
                    Name = request.Email.Split('@')[0], // Tạo username từ email
                    UserType = "Buyer",
                    Phone = request.Phone,
                    Address = request.Address,
                    RegistrationDate = DateTime.UtcNow,
                    Password = null // Đảm bảo password là null cho người dùng đăng nhập bằng Google
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"New user created: {user.Email}, UserId: {user.UserId}");
                var token = GenerateJwtToken(user);
                return Ok(new
                {
                    Token = token,
                    User = new { user.UserId, user.FullName, user.Name, user.Email, user.UserType }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during user registration");
                return StatusCode(500, $"An error occurred during registration: {ex.Message}");
            }
        }
        public class CompleteRegistrationRequest
        {
            public string Email { get; set; }
            public string FullName { get; set; }
            public string Phone { get; set; }
            public string Address { get; set; }
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
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Role, user.UserType)
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}