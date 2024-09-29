using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI_FlowerShopSWP.Models;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Globalization;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;
        private readonly ILogger<OrdersController> _logger;
        public OrdersController(FlowerEventShopsContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Orders
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            return await _context.Orders.ToListAsync();
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            return order;
        }


        [HttpPost("addtocart")]
        [Authorize]

        public async Task<IActionResult> AddToCart(int flowerId, int quantity)
        {
            // Get the userId from the token
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine($"User ID from token: {userIdClaim}"); // Log the user ID

            if (userIdClaim == null)
            {
                return BadRequest("Người dùng không tồn tại.");
            }

            int userId = int.Parse(userIdClaim); // Convert to int

            // Check if the user exists
            var existingUser = await _context.Users.FindAsync(userId);
            if (existingUser == null)
            {
                return BadRequest("Người dùng không tồn tại.");
            }

            // Check if the user has a pending order
            var existingOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.UserId == userId && o.OrderStatus == "Pending");

            if (existingOrder == null)
            {
                // Create a new order if none exists
                existingOrder = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    OrderStatus = "Pending",
                    DeliveryAddress = "Default Address"
                };
                _context.Orders.Add(existingOrder);
                await _context.SaveChangesAsync(); // Save to get orderId
            }

            // Add flower to the order
            var orderItem = new OrderItem
            {
                OrderId = existingOrder.OrderId,
                FlowerId = flowerId,
                Quantity = quantity,
                Price = (await _context.Flowers.FindAsync(flowerId)).Price // Get flower price
            };

            _context.OrderItems.Add(orderItem); // Add to OrderItems table
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sản phẩm đã được thêm vào giỏ hàng." });
        }



        [HttpPost]
        public async Task<ActionResult<Order>> PostOrder(Order order)
        {
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetOrder", new { id = order.OrderId }, order);
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                _logger.LogInformation("Checkout method called");

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    _logger.LogWarning("User ID claim not found");
                    return Unauthorized("User ID claim not found");
                }

                if (!int.TryParse(userIdClaim.Value, out int userId))
                {
                    _logger.LogWarning("Invalid user ID format");
                    return BadRequest("Invalid user ID format");
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning($"User with ID {userId} not found");
                    return NotFound($"User with ID {userId} not found");
                }

                // Rest of your checkout logic...

                return Ok(new { message = "Checkout successful" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during checkout");
                return StatusCode(500, "An error occurred during checkout");
            }
        }


        // PUT: api/Orders/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            if (id != order.OrderId)
            {
                return BadRequest();
            }

            _context.Entry(order).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
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

        // POST: api/Orders
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
       
        [HttpPut("updatecartitem")]
        public async Task<IActionResult> UpdateCartItem(int orderItemId, int quantity)
        {
            var orderItem = await _context.OrderItems.FindAsync(orderItemId);
            if (orderItem == null)
            {
                return NotFound("Sản phẩm không tồn tại trong giỏ hàng.");
            }

            orderItem.Quantity = quantity;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Số lượng sản phẩm đã được cập nhật." });
        }

        [HttpDelete("removecartitem/{orderItemId}")]
        public async Task<IActionResult> RemoveCartItem(int orderItemId)
        {
            var orderItem = await _context.OrderItems.FindAsync(orderItemId);
            if (orderItem == null)
            {
                return NotFound("Sản phẩm không tồn tại trong giỏ hàng.");
            }

            _context.OrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sản phẩm đã được xóa khỏi giỏ hàng." });
        }

        // DELETE: api/Orders/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }
}
