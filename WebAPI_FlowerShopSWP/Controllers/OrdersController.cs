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
using System.Text.Json;

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
            _logger.LogInformation($"AddToCart called with flowerId: {flowerId}, quantity: {quantity}");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation($"UserIdClaim: {userIdClaim}");

            if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.LogWarning($"Invalid user ID: {userIdClaim}");
                return BadRequest("Người dùng không hợp lệ.");
            }

            _logger.LogInformation($"UserId parsed: {userId}");

            var flower = await _context.Flowers.FindAsync(flowerId);
            if (flower == null)
            {
                _logger.LogWarning($"Flower not found for ID: {flowerId}");
                return NotFound("Không tìm thấy sản phẩm.");
            }

            _logger.LogInformation($"Flower found: {flower.FlowerName}, Available quantity: {flower.Quantity}");

            if (flower.Quantity < quantity)
            {
                _logger.LogWarning($"Insufficient quantity. Requested: {quantity}, Available: {flower.Quantity}");
                return BadRequest("Số lượng sản phẩm không đủ.");
            }

            var cartItem = new
            {
                FlowerId = flowerId,
                FlowerName = flower.FlowerName,
                Quantity = quantity,
                Price = flower.Price
            };

            _logger.LogInformation($"Cart item created: {JsonSerializer.Serialize(cartItem)}");

            return Ok(new { message = "Sản phẩm đã được thêm vào giỏ hàng.", cartItem });
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
        [Authorize]
        public async Task<IActionResult> Checkout([FromBody] List<CartItem> cartItems)
        {
            if (cartItems == null || !cartItems.Any())
            {
                return BadRequest("Giỏ hàng trống hoặc không hợp lệ");
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    {
                        return Unauthorized("ID người dùng không hợp lệ");
                    }

                    var user = await _context.Users.FindAsync(userId);
                    if (user == null)
                    {
                        return NotFound($"Không tìm thấy người dùng với ID {userId}");
                    }

                    var newOrder = new Order
                    {
                        UserId = userId,
                        OrderStatus = "Pending",
                        OrderDate = DateTime.Now,
                        DeliveryAddress = "Default Address", // Bạn có thể lấy địa chỉ từ input của người dùng
                        OrderItems = new List<OrderItem>()
                    };

                    decimal totalAmount = 0;
                    foreach (var cartItem in cartItems)
                    {
                        var flower = await _context.Flowers.FindAsync(cartItem.FlowerId);
                        if (flower == null)
                        {
                            return BadRequest($"Không tìm thấy hoa với ID {cartItem.FlowerId}");
                        }

                        if (flower.Quantity < cartItem.Quantity)
                        {
                            return BadRequest($"Không đủ số lượng hoa: {flower.FlowerName}");
                        }

                        flower.Quantity -= cartItem.Quantity;
                        totalAmount += flower.Price * cartItem.Quantity;

                        newOrder.OrderItems.Add(new OrderItem
                        {
                            FlowerId = flower.FlowerId,
                            FlowerName = flower.FlowerName,
                            Quantity = cartItem.Quantity,
                            Price = flower.Price
                        });
                    }

                    newOrder.TotalAmount = totalAmount;

                    _context.Orders.Add(newOrder);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    _logger.LogInformation("Order created successfully: OrderId={OrderId}, TotalAmount={TotalAmount}", newOrder.OrderId, totalAmount);

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
            await _context.SaveChangesAsync(); return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }

}