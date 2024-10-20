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

        [HttpPost("login")]
        public async Task<ActionResult<object>> Login([FromBody] LoginModel loginUser)
        {
            if (loginUser == null || string.IsNullOrEmpty(loginUser.Name) || string.IsNullOrEmpty(loginUser.Password))
            {
                return BadRequest("Invalid login data");
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Name == loginUser.Name);

            if (user == null || user.Password != loginUser.Password)
            {
                return Unauthorized("Invalid username or password");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:SecretKey"]);

            var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Name, user.Name),
        new Claim(ClaimTypes.Role, user.UserType)
    };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new
            {
                token = tokenHandler.WriteToken(token),
                userType = user.UserType,
                userId = user.UserId
            });
        }

        public class LoginModel
        {
            public string Name { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("register")]
        public async Task<ActionResult<User>> Register([FromBody] User newUser)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { Field = x.Key, Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToArray() })
                    .ToArray();

                return BadRequest(new { Message = "Validation failed", Errors = errors });
            }
            newUser.Followers = null;
            newUser.Following = null;

            if (_context.Users.Any(u => u.Email == newUser.Email))
            {
                return Conflict("Email already in use.");
            }

            if (string.IsNullOrWhiteSpace(newUser.Name) || string.IsNullOrWhiteSpace(newUser.FullName) ||
                string.IsNullOrWhiteSpace(newUser.Email) || string.IsNullOrWhiteSpace(newUser.Password) ||
                string.IsNullOrWhiteSpace(newUser.Address))
            {
                return BadRequest("All fields are required.");
            }

            newUser.UserType = "Buyer";
            newUser.RegistrationDate = DateTime.UtcNow;

            // Tìm UserId lớn nhất hiện tại
            var maxUserId = await _context.Users.MaxAsync(u => (int?)u.UserId) ?? 0;
            newUser.UserId = maxUserId + 1;

            _context.Users.Add(newUser);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while registering the user. Please try again.");
            }

            // Không trả về mật khẩu trong response
            newUser.Password = null;

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
        public async Task<IActionResult> UpdateUserProfile([FromForm] User updatedUser)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            user.Name = updatedUser.Name;
            user.Email = updatedUser.Email;
            user.FullName = updatedUser.FullName;
            user.Phone = updatedUser.Phone;
            user.Address = updatedUser.Address;

            
            if (!string.IsNullOrEmpty(updatedUser.ProfileImageUrl))
            {
                user.ProfileImageUrl = updatedUser.ProfileImageUrl;
            }

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
        [Authorize]
        [HttpPost("upload-profile-image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }


            var uploadsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "PersistentImages", "profile");


            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }


            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);


            var filePath = Path.Combine(uploadsFolderPath, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }


            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            user.ProfileImageUrl = "/images/profile/" + uniqueFileName;
            await _context.SaveChangesAsync();

            return Ok(new { message = "File uploaded successfully.", profileImageUrl = user.ProfileImageUrl });
        }

        // POST: api/Users
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
        [HttpGet("current-user")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                return BadRequest("Invalid user ID");
            }

            var user = await _context.Users
                .AsNoTracking()
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    Name = u.Name,
                    FullName = u.FullName,
                    Email = u.Email,
                    UserType = u.UserType,
                    Address = u.Address,
                    Phone = u.Phone,
                    RegistrationDate = u.RegistrationDate
                })
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/usertype")]
        public async Task<IActionResult> UpdateUserType(int id, [FromBody] string userType)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.UserType = userType;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        [HttpGet("revenue/{sellerId}")]
        public async Task<IActionResult> GetRevenue(int sellerId)
        {
            var completedOrderItems = await _context.OrderItems
                .Where(oi => oi.Flower.UserId == sellerId && oi.Order.OrderStatus == "Completed")
                .Include(oi => oi.Order)
                .Include(oi => oi.Flower)
                .ToListAsync();

            var totalRevenue = completedOrderItems.Sum(oi => oi.Price * oi.Quantity);
            var commission = totalRevenue * (decimal)0.30;
            var netRevenue = totalRevenue - commission; 

            var revenueDetails = completedOrderItems
                .Where(oi => oi.Order.OrderDate.HasValue)
                .GroupBy(oi => new { Day = oi.Order.OrderDate.Value.Date })
                .Select(g => new
                {
                    Date = g.Key.Day.ToString("dd/MM/yyyy"),
                    Amount = g.Sum(oi => oi.Price * oi.Quantity)
                })
                .ToList();

            var result = new
            {
                TotalRevenue = totalRevenue,
                Commission = commission,
                NetRevenue = netRevenue,
                Details = revenueDetails
            };

            return Ok(result);
        }

        [Authorize]
        [HttpPost]
        [Route("api/withdrawal")]
        public IActionResult CreateWithdrawalRequest([FromBody] WithdrawalRequest request)
        {
            if (request == null ||
                string.IsNullOrEmpty(request.AccountNumber) ||
                string.IsNullOrEmpty(request.FullName) ||
                string.IsNullOrEmpty(request.Phone) ||
                request.Amount <= 0)
            {
                return BadRequest("Vui lòng cung cấp đầy đủ thông tin hợp lệ.");
            }

            request.Remarks ??= null; 

            
            request.Status = "Pending";
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                request.UserId = int.Parse(userIdClaim.Value);
            }


            using (var context = new FlowerEventShopsContext())
            {
                context.WithdrawalRequests.Add(request);
                context.SaveChanges(); 
            }

            return Ok("Yêu cầu rút tiền đã được gửi.");
        }









        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePassword model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng.");
            }

            if (user.Password != model.CurrentPassword)
            {
                return BadRequest("Mật khẩu hiện tại không đúng.");
            }

            if (model.NewPassword.Length < 5)
            {
                return BadRequest("Mật khẩu mới phải có ít nhất 5 ký tự.");
            }
            user.Password = model.NewPassword;

            try
            {
                await _context.SaveChangesAsync();
                return Ok("Đổi mật khẩu thành công.");
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "Đã xảy ra lỗi khi đổi mật khẩu.");
            }
        }
        [Authorize]
        [HttpGet("api/users/{userId}/revenue")]
        public async Task<IActionResult> GetUserRevenue(int userId)
        {
            using (var context = new FlowerEventShopsContext())
            {
                
                var totalRevenue = await context.OrderItems
                    .Where(oi => oi.Flower.UserId == userId && oi.Order.OrderStatus == "Completed")
                    .SumAsync(oi => oi.Price * oi.Quantity);

                
                var commission = totalRevenue * (decimal)0.30;

                
                var totalWithdrawn = await context.WithdrawalRequests
                    .Where(wr => wr.UserId == userId && wr.Status == "Approved")
                    .SumAsync(wr => wr.Amount);

               
                var currentIncome = totalRevenue - commission - totalWithdrawn; 

                return Ok(new
                {
                    UserId = userId,
                    TotalRevenue = totalRevenue,
                    Commission = commission,
                    TotalWithdrawn = totalWithdrawn,
                    CurrentIncome = currentIncome
                });
            }
        }
        [Authorize]
        [HttpGet("api/withdrawal-requests/{userId}")]
        public IActionResult GetWithdrawalRequests(int userId)
        {
            using (var context = new FlowerEventShopsContext())
            {
                var requests = context.WithdrawalRequests
                    .Where(r => r.UserId == userId) 
                    .ToList();
                return Ok(requests);
            }
        }
        [Authorize]
        [HttpDelete("api/withdrawal-requests/{requestId}")]
        public IActionResult CancelWithdrawalRequest(int requestId)
        {
            using (var context = new FlowerEventShopsContext())
            {
                var request = context.WithdrawalRequests.Find(requestId);
                if (request == null)
                {
                    return NotFound();
                }

                context.WithdrawalRequests.Remove(request);
                context.SaveChanges();
                return Ok();
            }
        }


        public class UserDto
        {
            public int UserId { get; set; }
            public string Name { get; set; }
            public string FullName { get; set; }
            public string Email { get; set; }
            public string UserType { get; set; }
            public string Address { get; set; }
            public string Phone { get; set; }
            public DateTime? RegistrationDate { get; set; }
        }

    }
}