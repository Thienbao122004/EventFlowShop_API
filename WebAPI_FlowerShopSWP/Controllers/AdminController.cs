using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;

        public AdminController(FlowerEventShopsContext context)
        {
            _context = context;
        }

        // Get all users
        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            return await _context.Users.ToListAsync();
        }

        // Get all flowers
        [HttpGet("flowers")]
        public async Task<ActionResult<IEnumerable<Flower>>> GetAllFlowers()
        {
            return await _context.Flowers.Include(f => f.Category).ToListAsync();
        }

        // Delete a user
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Bước 1: Lấy tất cả các đơn hàng của người dùng
                var orders = await _context.Orders
                    .Where(o => o.UserId == id)
                    .Include(o => o.OrderItems) // Lấy các OrderItems liên quan
                    .Include(o => o.Payments) // Lấy các Payments liên quan
                    .ToListAsync();

                // Bước 2: Xóa OrderItems và Payments liên quan đến các đơn hàng
                foreach (var order in orders)
                {
                    
                    _context.OrderItems.RemoveRange(order.OrderItems);

                    
                    _context.Payments.RemoveRange(order.Payments);
                }

                // Bước 3: Xóa các đơn hàng
                _context.Orders.RemoveRange(orders);
                await _context.SaveChangesAsync();

                // Bước 4: Xóa các đánh giá của người dùng
                var reviews = await _context.Reviews.Where(r => r.UserId == id).ToListAsync();
                _context.Reviews.RemoveRange(reviews);
                await _context.SaveChangesAsync();

                // Bước 5: Xóa các thông báo của người dùng
                var notifications = await _context.Notifications.Where(n => n.UserId == id).ToListAsync();
                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();

                // Bước 6: Cuối cùng xóa người dùng
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, "An error occurred while deleting the user.");
            }
        }

        // Update flower status
        [HttpPut("flowers/{id}")]
        public async Task<IActionResult> UpdateFlowerStatus(int id, Flower updatedFlower)
        {
            var flower = await _context.Flowers.FindAsync(id);
            if (flower == null)
            {
                return NotFound();
            }

            flower.Status = updatedFlower.Status;
            await _context.SaveChangesAsync();
            return NoContent();
        }
        // cập nhật role của người dùng 
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] string newRole)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.UserType = newRole;
            await _context.SaveChangesAsync();

            return NoContent();
        }
        // Get all orders
        [HttpGet("orders")]
        public async Task<ActionResult<IEnumerable<Order>>> GetAllOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)    // Lấy các OrderItems liên quan
                .Include(o => o.Payments)      // Lấy các Payments liên quan
                .Include(o => o.User)          // Lấy thông tin User dựa trên UserId
                .ToListAsync();

            return Ok(orders);
        }
    }
}
