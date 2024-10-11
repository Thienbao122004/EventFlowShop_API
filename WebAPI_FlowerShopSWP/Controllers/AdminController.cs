using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Authorize(Roles = "Admin")]
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

        //// Update flower status
        //[HttpPut("flowers/{id}")]
        //public async Task<IActionResult> UpdateFlowerStatus(int id, Flower updatedFlower)
        //{
        //    var flower = await _context.Flowers.FindAsync(id);
        //    if (flower == null)
        //    {
        //        return NotFound();
        //    }

        //    flower.Status = updatedFlower.Status;
        //    await _context.SaveChangesAsync();
        //    return NoContent();
        //}
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
                .ThenInclude(oi => oi.Flower)   // Lấy thông tin hoa
                .Include(o => o.User)           // Lấy thông tin User dựa trên UserId
                .ToListAsync();

            return Ok(orders);
        }
        // Xóa hoa
        [HttpDelete("flowers/{id}")]
        public async Task<IActionResult> DeleteFlower(int id)
        {
            var flower = await _context.Flowers.FindAsync(id);
            if (flower == null)
            {
                return NotFound();
            }

            _context.Flowers.Remove(flower);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        // Cập nhật thông tin người dùng
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User updatedUser)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Name = updatedUser.Name ?? user.Name;
            user.Email = updatedUser.Email ?? user.Email;
            user.UserType = updatedUser.UserType ?? user.UserType;
            user.Phone = updatedUser.Phone ?? user.Phone;
            user.Address = updatedUser.Address ?? user.Address;

            // Cập nhật mật khẩu chỉ khi nó không phải là null hoặc trống
            if (!string.IsNullOrWhiteSpace(updatedUser.Password))
            {
                user.Password = updatedUser.Password;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }
        // Cập nhật thông tin hoa
        [HttpPut("flowers/{id}")]
        public async Task<IActionResult> UpdateFlower(int id, [FromBody] Flower updatedFlower)
        {
            var flower = await _context.Flowers.FindAsync(id);
            if (flower == null)
            {
                return NotFound();
            }

            flower.FlowerName = updatedFlower.FlowerName;
            flower.Quantity = updatedFlower.Quantity;
            flower.Condition = updatedFlower.Condition;
            flower.Status = updatedFlower.Status;


            await _context.SaveChangesAsync();
            return NoContent();
        }
        // Cập nhật trạng thái đơn hàng
        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.OrderStatus = newStatus; // Cập nhật trạng thái đơn hàng
            await _context.SaveChangesAsync();

            return NoContent();
        }
        // Xóa đơn hàng
        [HttpDelete("orders/{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Payments) // Giả sử bạn đã có quan hệ giữa Orders và Payments
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            // Xóa các bản ghi thanh toán liên quan
            _context.Payments.RemoveRange(order.Payments);

            // Xóa đơn hàng
            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Lấy thông tin chi tiết đơn hàng
        [HttpGet("orders/{id}")]
        public async Task<ActionResult<Order>> GetOrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Flower) // Lấy thông tin hoa
                .Include(o => o.User) // Lấy thông tin người dùng
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            // Cấu trúc lại order để bao gồm thông tin OrderItems
            var orderWithItems = new
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                UserName = order.User.Name, // Hoặc thông tin khác từ User
                OrderItems = order.OrderItems.Select(oi => new
                {
                    OrderItemId = oi.OrderItemId,
                    FlowerId = oi.FlowerId,
                    FlowerName = oi.Flower.FlowerName, // Lấy tên hoa từ bảng Flowers
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            };

            return Ok(orderWithItems);
        }


        [HttpGet("dashboard/stats")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var totalIncome = await _context.OrderItems.SumAsync(item => item.Price * item.Quantity);
            var totalProducts = await _context.Flowers.CountAsync();

            var stats = new
            {
                TotalOrders = totalOrders,
                TotalIncome = totalIncome,
                TotalProducts = totalProducts
            };

            return Ok(stats);
        }
        [HttpGet("dashboard/income")]
        public async Task<IActionResult> GetDailyIncome()
        {
            var dailyIncome = await _context.Orders
                .Where(o => o.OrderDate.HasValue)
                .GroupBy(o => o.OrderDate.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Income = g.SelectMany(o => o.OrderItems)
                               .Sum(item => item.Price * item.Quantity) // Tính tổng thu nhập từ các OrderItems
                })
                .ToListAsync();

            return Ok(dailyIncome);
        }





    }
}
