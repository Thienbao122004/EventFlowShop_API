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
    public enum OrderDelivery
    {
        ChờXửLý,
        ĐangXửLý,
        ĐãGửiHàng,
        ĐãGiaoHàng,
        ĐãHủy
    }
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

        [HttpGet("history")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<object>>> GetOrderHistory()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("Invalid user ID");
            }

            var orders = await _context.Orders
               .Where(o => o.UserId == userId)
               .Include(o => o.OrderItems)
               .Include(o => o.User)
               .OrderByDescending(o => o.OrderDate)
               .Select(o => new
               {
                   o.OrderId,
                   o.OrderStatus,
                   o.OrderDate,
                   o.TotalAmount,
                   o.OrderDelivery, // Thêm dòng này
                   OrderItems = o.OrderItems.Select(oi => new
                   {
                       oi.FlowerId,
                       oi.FlowerName,
                       oi.Quantity,
                       oi.Price
                   }),
                   Recipient = new
                   {
                       FullName = o.User.Name,
                       Phone = o.User.Phone,
                       Email = o.User.Email,
                       Address = o.DeliveryAddress
                   },
                   User = new
                   {
                       FullName = o.User.Name,
                       Phone = o.User.Phone,
                       Email = o.User.Email
                   }
               })
               .ToListAsync();

                    return Ok(orders);
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

        [HttpGet("seller-orders/{userId}")]
        [Authorize(Roles = "Seller")]
        public async Task<ActionResult<IEnumerable<object>>> GetSellerOrders(int userId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    return Unauthorized("Invalid user ID");
                }

                if (currentUserId != userId)
                {
                    return Forbid("You are not authorized to view these orders");
                }

                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Flower)
                    .Where(o => o.OrderItems.Any(oi => oi.Flower.UserId == userId))
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new
                    {
                        o.OrderId,
                        o.OrderStatus,
                        o.OrderDate,
                        o.TotalAmount,
                        OrderDelivery = o.OrderDelivery.HasValue ? o.OrderDelivery.Value.ToString() : "N/A",
                        Items = o.OrderItems.Where(oi => oi.Flower.UserId == userId).Select(oi => new
                        {
                            oi.FlowerId,
                            oi.FlowerName,
                            oi.Quantity,
                            oi.Price
                        }),
                        Recipient = new
                        {
                            FullName = o.User.Name,
                            Phone = o.User.Phone,
                            Email = o.User.Email,
                            Address = o.DeliveryAddress
                        },
                        User = new
                        {
                            FullName = o.User.Name,
                            Phone = o.User.Phone,
                            Email = o.User.Email
                        }
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching seller orders");
                return StatusCode(500, "An error occurred while processing your request");
            }
        }

        [HttpPut("{orderId}/delivery")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateOrderDelivery(int orderId, [FromBody] UpdateOrderDeliveryDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int currentUserId))
                {
                    return Unauthorized("Invalid user ID");
                }

                if (currentUserId != dto.UserId) 
                {
                    return Forbid("You are not authorized to update this order");
                }

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Flower)
                    .FirstOrDefaultAsync(o => o.OrderId == orderId && o.OrderItems.Any(oi => oi.Flower.UserId == currentUserId));

                if (order == null)
                {
                    return NotFound("Order not found or you don't have permission to update this order");
                }

                if (!Enum.TryParse(dto.OrderDelivery, out OrderDelivery newDeliveryStatus))
                {
                    return BadRequest("Invalid order delivery status");
                }

                order.OrderDelivery = newDeliveryStatus;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Order delivery status updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating order delivery status for OrderId: {orderId}");
                return StatusCode(500, $"An error occurred while updating the order: {ex.Message}");
            }
        }

        public class UpdateOrderDeliveryDto
        {
            public string OrderDelivery { get; set; }
            public int UserId { get; set; } // Thay đổi từ SellerId sang UserId
        }

        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout([FromBody] List<CartItem> cartItems, [FromQuery] string fullAddress)
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
                        //DeliveryAddress = user.Address,
                        DeliveryAddress = string.IsNullOrEmpty(fullAddress) ? user.Address : fullAddress,
                        OrderDelivery = OrderDelivery.ChờXửLý,
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
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.Payments)
                
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound();
            }
            // Xóa tất cả các mục trong bảng Payments
            _context.Payments.RemoveRange(order.Payments);

            // Xóa tất cả các mục trong bảng Order_Items
            _context.OrderItems.RemoveRange(order.OrderItems);

            // Xóa đơn hàng
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            return NoContent();
        }





        [HttpGet("orders/seller/{sellerId}")]
        public async Task<IActionResult> GetOrdersBySeller(int sellerId)
        {
            var orders = await _context.OrderItems
                .Where(oi => oi.Flower.UserId == sellerId && oi.Order.OrderStatus == "Completed")
                .Include(oi => oi.Order)
                .Include(oi => oi.Order.User)
                .Select(oi => new
                {
                    oi.Order.OrderId,
                    oi.Order.OrderDate,
                    oi.Order.OrderStatus,
                    oi.Order.DeliveryAddress,
                    OrderDelivery = oi.Order.OrderDelivery.HasValue ? oi.Order.OrderDelivery.Value.ToString() : "N/A",
                    oi.Order.TotalAmount,
                    BuyerName = oi.Order.User.FullName,
                    BuyerEmail = oi.Order.User.Email,
                    BuyerPhone = oi.Order.User.Phone,
                    ProductName = oi.Flower.FlowerName,
                    Quantity = oi.Quantity,
                    ItemTotal = oi.Quantity * oi.Price
                })
                .ToListAsync();

            return Ok(orders);
        }
        [HttpPut("update/{orderId}")]
        [Authorize]
        public async Task<IActionResult> UpdateOrder(int orderId, [FromBody] UpdateOrderDto updateOrderDto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("Invalid user ID");
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound("Order not found or you don't have permission to update this order");
            }

            order.DeliveryAddress = updateOrderDto.DeliveryAddress;
            foreach (var item in order.OrderItems)
            {
                item.Quantity = updateOrderDto.Quantity; 
            }
            order.TotalAmount = order.OrderItems.Sum(oi => oi.Price * updateOrderDto.Quantity); 


            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.Email = updateOrderDto.BuyerEmail;
                user.Phone = updateOrderDto.BuyerPhone;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }
        public class UpdateOrderDto
        {
            public string DeliveryAddress { get; set; }
            public int Quantity { get; set; }
            public string BuyerEmail { get; set; }
            public string BuyerPhone { get; set; }
        }



        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.OrderId == id);
        }
    }

}