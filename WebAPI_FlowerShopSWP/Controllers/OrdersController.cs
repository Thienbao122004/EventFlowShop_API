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
using WebAPI_FlowerShopSWP.Repository;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;
        private readonly ILogger<OrdersController> _logger;
        private readonly IEmailSender _emailSender;

        public OrdersController(FlowerEventShopsContext context, ILogger<OrdersController> logger, IEmailSender emailSender)
        {
            _context = context;
            _logger = logger;
            _emailSender = emailSender;
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

        public class CartItem
        {
            public int FlowerId { get; set; }
            public int Quantity { get; set; }
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] List<CartItem> cartItems)
        {
            _logger.LogInformation("Checkout method called with {CartItemCount} items", cartItems?.Count ?? 0);

            if (cartItems == null || !cartItems.Any())
            {
                _logger.LogWarning("Cart is empty or invalid");
                return BadRequest("Giỏ hàng trống hoặc không hợp lệ");
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    {
                        _logger.LogWarning("Invalid user ID: {ClaimValue}", userIdClaim?.Value);
                        return Unauthorized("ID người dùng không hợp lệ");
                    }

                    var user = await _context.Users.FindAsync(userId);
                    if (user == null)
                    {
                        _logger.LogWarning("User with ID {UserId} not found", userId);
                        return NotFound($"Không tìm thấy người dùng với ID {userId}");
                    }

                    var newOrder = new Order
                    {
                        UserId = userId,
                        OrderStatus = "Complete",
                        OrderDate = DateTime.Now,
                        DeliveryAddress = "Default Address", // You may want to get this from the user input
                        OrderItems = new List<OrderItem>()
                    };

                    decimal totalAmount = 0;
                    foreach (var cartItem in cartItems)
                    {
                        var flower = await _context.Flowers.FindAsync(cartItem.FlowerId);
                        if (flower == null)
                        {
                            _logger.LogWarning("Flower not found: FlowerId={FlowerId}", cartItem.FlowerId);
                            return BadRequest($"Không tìm thấy hoa với ID {cartItem.FlowerId}");
                        }

                        if (flower.Quantity < cartItem.Quantity)
                        {
                            _logger.LogWarning("Insufficient quantity for flower: FlowerId={FlowerId}, Available={Available}, Requested={Requested}",
                                flower.FlowerId, flower.Quantity, cartItem.Quantity);
                            return BadRequest($"Không đủ số lượng hoa: {flower.FlowerName}");
                        }

                        flower.Quantity -= cartItem.Quantity;
                        totalAmount += flower.Price * cartItem.Quantity;

                        newOrder.OrderItems.Add(new OrderItem
                        {
                            FlowerId = flower.FlowerId,
                            Quantity = cartItem.Quantity,
                            Price = flower.Price
                        });
                    }

                    newOrder.TotalAmount = totalAmount;

                    _context.Orders.Add(newOrder);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation("Order created successfully: OrderId={OrderId}, TotalAmount={TotalAmount}", newOrder.OrderId, totalAmount);

                    // Send confirmation email
                    try
                    {
                        await SendConfirmationEmail(user, newOrder, totalAmount);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Error sending confirmation email");
                        // Continue processing even if email sending fails
                    }

                    return Ok(new { message = "Đặt hàng thành công", orderId = newOrder.OrderId, totalAmount });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Unexpected error during checkout: {ErrorMessage}", ex.Message);
                    if (ex.InnerException != null)
                    {
                        _logger.LogError("Inner exception: {InnerErrorMessage}", ex.InnerException.Message);
                    }
                    return StatusCode(500, $"Đã xảy ra lỗi không mong muốn: {ex.Message}");
                }
            }
        }

        private async Task SendConfirmationEmail(User user, Order order, decimal totalAmount)
        {
            string emailSubject = "Xác nhận đơn hàng";
            string emailBody = $@"
    Xin chào {user.Name},

    Đơn hàng của bạn đã được xác nhận thành công.

    Chi tiết đơn hàng:
    - Mã đơn hàng: {order.OrderId}
    - Ngày đặt hàng: {order.OrderDate:dd/MM/yyyy HH:mm}
    - Tổng giá trị: {totalAmount:N0} VNĐ

    Cảm ơn bạn đã mua hàng tại cửa hàng chúng tôi!

    Trân trọng,
    Đội ngũ hỗ trợ khách hàng";

            await _emailSender.SendEmailAsync(user.Email, emailSubject, emailBody);
            _logger.LogInformation("Confirmation email sent to {UserEmail}", user.Email);
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