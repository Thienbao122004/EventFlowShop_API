using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
                var orders = await _context.Orders
                    .Where(o => o.UserId == id)
                    .Include(o => o.OrderItems)
                    .Include(o => o.Payments)
                    .ToListAsync();

                foreach (var order in orders)
                {
                    _context.OrderItems.RemoveRange(order.OrderItems);
                    _context.Payments.RemoveRange(order.Payments);
                }

                _context.Orders.RemoveRange(orders);
                await _context.SaveChangesAsync();

                var reviews = await _context.Reviews.Where(r => r.UserId == id).ToListAsync();
                _context.Reviews.RemoveRange(reviews);
                await _context.SaveChangesAsync();

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

        // Update user role
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
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Flower)
                .Include(o => o.User)
                .ToListAsync();

            return Ok(orders);
        }

        // Delete flower
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

        // Update user information
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] User updatedUser)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Name = updatedUser.Name ?? user.Name;
            user.FullName = updatedUser.FullName ?? user.FullName;
            user.Email = updatedUser.Email ?? user.Email;
            user.UserType = updatedUser.UserType ?? user.UserType;
            user.Phone = updatedUser.Phone ?? user.Phone;
            user.Address = updatedUser.Address ?? user.Address;

            if (!string.IsNullOrWhiteSpace(updatedUser.Password))
            {
                user.Password = updatedUser.Password;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // Update flower information
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

        // Update order status
        [HttpPut("orders/{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.OrderStatus = newStatus;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Delete order
        [HttpDelete("orders/{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            _context.Payments.RemoveRange(order.Payments);
            _context.Orders.Remove(order);

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Get order details
        [HttpGet("orders/{id}")]
        public async Task<ActionResult<Order>> GetOrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Flower)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            var orderWithItems = new
            {
                OrderId = order.OrderId,
                OrderDate = order.OrderDate,
                UserName = order.User.Name,
                OrderItems = order.OrderItems.Select(oi => new
                {
                    OrderItemId = oi.OrderItemId,
                    FlowerId = oi.FlowerId,
                    FlowerName = oi.Flower.FlowerName,
                    Quantity = oi.Quantity,
                    Price = oi.Price
                }).ToList()
            };

            return Ok(orderWithItems);
        }

        // Get dashboard stats
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
                               .Sum(item => item.Price * item.Quantity)
                })
                .ToListAsync();

            return Ok(dailyIncome);
        }
        [Authorize]
        [HttpGet("api/withdrawal-requests")]
        public IActionResult GetWithdrawalRequests()
        {
            using (var context = new FlowerEventShopsContext())
            {
                var requests = context.WithdrawalRequests.ToList();
                return Ok(requests);
            }
        }

        [Authorize]
        [HttpPut("api/withdrawal-request/{id}")]
        public IActionResult UpdateWithdrawalRequest(int id, [FromBody] string status)
        {
            using (var context = new FlowerEventShopsContext())
            {
                var request = context.WithdrawalRequests.Find(id);
                if (request == null)
                {
                    return NotFound();
                }

                request.Status = status;
                context.SaveChanges();

                return Ok("Trạng thái yêu cầu đã được cập nhật.");
            }
        }
        [Authorize]
        [HttpPost("api/withdrawals/{requestId}/approve")]
        public async Task<IActionResult> ApproveWithdrawalRequest(int requestId)
        {
            using (var context = new FlowerEventShopsContext())
            {
                var request = await context.WithdrawalRequests.FindAsync(requestId);
                if (request == null)
                {
                    return NotFound("Yêu cầu không tồn tại.");
                }
       
                request.Status = "Approved";

                var user = await context.Users.FindAsync(request.UserId);
                if (user == null)
                {
                    return NotFound("Người dùng không tồn tại.");
                }

                
                var totalRevenue = await context.OrderItems
                    .Where(oi => oi.Flower.UserId == user.UserId && oi.Order.OrderStatus == "Completed")
                    .SumAsync(oi => oi.Price * oi.Quantity);

                
                if (totalRevenue < request.Amount)
                {
                    return BadRequest("Doanh thu không đủ để thực hiện yêu cầu rút tiền.");
                }

                totalRevenue -= request.Amount;


                await context.SaveChangesAsync();

                return Ok("Yêu cầu đã được duyệt và doanh thu đã được cập nhật.");
            }
        }


    }
}