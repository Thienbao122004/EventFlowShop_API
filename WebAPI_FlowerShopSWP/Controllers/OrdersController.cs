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
using System.Drawing.Drawing2D;
using WebAPI_FlowerShopSWP.Services;
using WebAPI_FlowerShopSWP.Helpers;
using WebAPI_FlowerShopSWP.DTO;
using Microsoft.Data.SqlClient;

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
        private readonly INotificationService _notificationService;

        public OrdersController(
            FlowerEventShopsContext context,
            ILogger<OrdersController> logger,
            IEmailSender emailSender,
            INotificationService notificationService)
        {
            _context = context;
            _logger = logger;
            _emailSender = emailSender;
            _notificationService = notificationService;
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
                   o.OrderDelivery,
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

        public class UpdateOrderDeliveryDto
        {
            public string OrderDelivery { get; set; }
            public int UserId { get; set; }
        }

        [HttpPut("{orderId}/delivery")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> UpdateOrderDelivery(int orderId, [FromBody] UpdateOrderDeliveryDto dto)
        {
            try
            {
                if (!Enum.TryParse<OrderDelivery>(dto.OrderDelivery, out OrderDelivery newStatus))
                {
                    return BadRequest("Invalid delivery status");
                }

                using var command = _context.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT UserId FROM Orders WHERE OrderId = @orderId";
                command.Parameters.Add(new SqlParameter("@orderId", orderId));

                await _context.Database.OpenConnectionAsync();

                int? userId;
                using (var result = await command.ExecuteReaderAsync())
                {
                    if (!await result.ReadAsync())
                    {
                        return NotFound($"Order {orderId} not found");
                    }
                    userId = result.GetInt32(0);
                }

                var updateSql = @"
                    UPDATE Orders 
                    SET OrderDelivery = @status 
                    WHERE OrderId = @orderId";

                await _context.Database.ExecuteSqlRawAsync(
                    updateSql,
                    new SqlParameter("@status", newStatus.ToString()),
                    new SqlParameter("@orderId", orderId)
                );

                try
                {
                    if (userId.HasValue)
                    {
                        var notification = new CreateNotificationDTO
                        {
                            UserId = userId.Value,
                            Title = "Cập nhật đơn hàng",
                            Content = $"Đơn hàng #{orderId} {GetStatusMessage(newStatus)}",
                            Type = "ORDER_STATUS",
                            RelatedId = orderId,
                            RelatedType = "order"
                        };

                        await _notificationService.CreateNotification(notification);
                    }
                }
                catch (Exception notifEx)
                {
                    _logger.LogError(notifEx, "Failed to create notification");
                }

                return Ok(new
                {
                    message = "Order status updated successfully",
                    newStatus = newStatus.ToString()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return StatusCode(500, "An error occurred while updating the order status");
            }
        }

        private string GetStatusMessage(OrderDelivery status) => status switch
        {
            OrderDelivery.ChờXửLý => "đang chờ xử lý",
            OrderDelivery.ĐangXửLý => "đang được xử lý",
            OrderDelivery.ĐãGửiHàng => "đã được gửi đi",
            OrderDelivery.ĐãGiaoHàng => "đã được giao thành công",
            OrderDelivery.ĐãHủy => "đã bị hủy",
            _ => "đã được cập nhật"
        };

        public class UpdateOrderDeliveryDTO
        {
            public string OrderDelivery { get; set; }
            public int UserId { get; set; }
        }

        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> Checkout([FromBody] List<CartItem> cartItems, [FromQuery] string fullAddress, [FromQuery] string wardCode, [FromQuery] string wardName, [FromQuery] int toDistrictId, [FromQuery] string? note)
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
                        DeliveryAddress = string.IsNullOrEmpty(fullAddress) ? user.Address : fullAddress,
                        OrderDelivery = OrderDelivery.ChờXửLý,
                        WardCode = wardCode,
                        WardName = wardName,
                        ToDistrictId = toDistrictId,
                        Note = string.IsNullOrWhiteSpace(note) ? null : note,
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
                    _context.Orders.Add(newOrder);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Order created successfully: OrderId={OrderId}, TotalAmount={TotalAmount}", newOrder.OrderId, totalAmount);
                    var notification = NotificationHelper.CreateOrderNotification(
                        userId,
                        newOrder.OrderId,
                        "được tạo thành công"
                    );
                    await _notificationService.CreateNotification(notification);

                    return Ok(new { message = "Đặt hàng thành công", orderId = newOrder.OrderId, totalAmount });
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

        [HttpGet("details/{orderId}")]
        [Authorize]
        public async Task<ActionResult<object>> GetOrderDetails(int orderId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("Invalid user ID");
            }

            var order = await _context.Orders
              .Where(o => o.OrderId == orderId && o.UserId == userId)
              .Include(o => o.OrderItems)
              .Include(o => o.User)
              .Select(o => new
              {
                  o.OrderId,
                  o.OrderStatus,
                  o.OrderDate,
                  o.TotalAmount,
                  o.OrderDelivery,
                  o.DeliveryAddress,
                  o.WardCode,
                  o.WardName,
                  o.ToDistrictId,
                  o.Note,
                  FullName = o.User.Name,
                  Phone = o.User.Phone,
                  Email = o.User.Email,
                  Items = o.OrderItems.Select(oi => new
                  {
                      oi.FlowerId,
                      oi.FlowerName,
                      oi.Quantity,
                      oi.Price
                  }),
                  TotalWeight = o.OrderItems.Sum(oi => oi.Quantity * 5000)
              })
              .FirstOrDefaultAsync();
            _logger.LogInformation($"Retrieved order with ToDistrictId: {order?.ToDistrictId}");
            if (order == null)
            {
                return NotFound("Order not found or you don't have permission to view this order");
            }

            return Ok(order);
        }
    }
}