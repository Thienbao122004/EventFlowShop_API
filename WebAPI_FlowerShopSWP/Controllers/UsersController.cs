using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebAPI_FlowerShopSWP.Models;
using WebAPI_FlowerShopSWP.Repository;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailSender _emailSender;

        public UsersController(FlowerEventShopsContext context, IConfiguration configuration, IEmailSender emailSender)
        {
            _context = context;
            _configuration = configuration;
            _emailSender = emailSender;
        }

        // GET: api/Users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // GET: api/Users/5
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<User>> GetProfile()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // GET: api/Users/profile/{id}
        [HttpGet("profile/{id}")]
        public async Task<ActionResult<User>> ViewProfile(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        // POST: api/Users/login
        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] User loginUser)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Name == loginUser.Name && u.Password == loginUser.Password);

            if (user == null)
            {
                return Unauthorized();
            }

            // Tạo token
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.Name)
    };

            // Thêm vai trò vào claims dựa trên UserType
            switch (user.UserType)
            {
                case "Admin":
                    claims.Add(new Claim(ClaimTypes.Role, "Admin"));
                    break;
                case "Seller":
                    claims.Add(new Claim(ClaimTypes.Role, "Seller"));
                    break;
                // Thêm các trường hợp khác nếu cần
                default:
                    claims.Add(new Claim(ClaimTypes.Role, "User"));
                    break;
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // Trả về token và UserType cho client
            return Ok(new { Token = tokenString, user.UserType });
        }


        // POST: api/Users/register
        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromBody] User newUser)
        {
            // Check if the email already exists
            if (_context.Users.Any(u => u.Email == newUser.Email))
            {
                return Conflict("Email already in use.");
            }
            newUser.UserType = "Buyer";
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = newUser.UserId }, newUser);
        }



        // PUT: api/Users/update/{id}
        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateProfile(int id, User user)
        {
            if (id != user.UserId)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }



        // PUT: api/Users/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUser(int id, User user)
        {
            if (id != user.UserId)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [Authorize]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateUserProfile([FromBody] User updatedUser)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            user.Name = updatedUser.Name;
            user.Email = updatedUser.Email;
            user.Phone = updatedUser.Phone;
            user.Address = updatedUser.Address;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(userId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Ok(user);
        }

        // POST: api/Users
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetUser", new { id = user.UserId }, user);
        }

        public class ForgotPasswordModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordModel model)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                // Không tiết lộ liệu email có tồn tại hay không
                return Ok(new { message = "Nếu email tồn tại, bạn sẽ nhận được mật khẩu mới." });
            }

            // Tạo mật khẩu mới
            string newPassword = GenerateRandomPassword();
            user.Password = newPassword; // Lưu ý: Trong thực tế, bạn nên mã hóa mật khẩu

            await _context.SaveChangesAsync();

            // Gửi email với mật khẩu mới
            await SendNewPasswordEmail(user.Email, newPassword);

            return Ok(new { message = "Nếu email tồn tại, bạn sẽ nhận được mật khẩu mới." });
        }

        private string GenerateRandomPassword()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        private async Task SendNewPasswordEmail(string email, string newPassword)
        {
            string subject = "Mật khẩu mới cho tài khoản của bạn";
            string message = $"Mật khẩu mới của bạn là: {newPassword}";
            await _emailSender.SendEmailAsync(email, subject, message);
        }

        // DELETE: api/Users/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }
    }
}