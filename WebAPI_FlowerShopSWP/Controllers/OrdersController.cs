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
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;


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

        public class CartItemRequest
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

                if (currentUserId != dto.UserId) // Thay đổi từ SellerId sang UserId
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
        public async Task<IActionResult> Checkout([FromBody] List<CartItemRequest> cartItems, [FromQuery] string fullAddress, [FromQuery] string wardCode, [FromQuery] string wardName, [FromQuery] int toDistrictId, [FromQuery] string? note)
        {
            if (cartItems == null || !cartItems.Any())
            {
                return BadRequest("Giỏ hàng trống hoặc không hợp lệ");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
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
                    return NotFound("Không tìm thấy người dùng");
                }

                var flowerIds = cartItems.Select(ci => ci.FlowerId).Distinct().ToList();
                var flowers = await _context.Flowers
                    .Where(f => flowerIds.Contains(f.FlowerId))
                    .ToDictionaryAsync(f => f.FlowerId, f => f);

                var itemsBySeller = cartItems
                    .GroupBy(item => flowers[item.FlowerId].UserId)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var (sellerUserId, sellerItems) in itemsBySeller)
                {
                    var newOrder = new Order
                    {
                        UserId = userId,
                        OrderStatus = "Pending",
                        OrderDate = DateTime.UtcNow,
                        DeliveryAddress = string.IsNullOrEmpty(fullAddress) ? user.Address : fullAddress,
                        OrderDelivery = OrderDelivery.ChờXửLý,
                        WardCode = wardCode,
                        WardName = wardName,
                        ToDistrictId = toDistrictId,
                        Note = string.IsNullOrWhiteSpace(note) ? null : note,
                        OrderItems = new List<OrderItem>()
                    };

                    decimal totalAmount = 0;
                    foreach (var cartItem in sellerItems)
                    {
                        if (!flowers.TryGetValue(cartItem.FlowerId, out var flower))
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
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Đặt hàng thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected error during checkout: {ErrorMessage}", ex.Message);
                return StatusCode(500, $"Đã xảy ra lỗi không mong muốn: {ex.Message}");
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
                    .ThenInclude(oi => oi.Flower) // Thêm Include cho Flower
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
                        FlowerName = oi.Flower.FlowerName, // Lấy tên hoa từ relationship
                        oi.Quantity,
                        oi.Price
                    }),
                    TotalWeight = o.OrderItems.Sum(oi => oi.Quantity * 5000)
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                return NotFound("Order not found or you don't have permission to view this order");
            }

            return Ok(order);
        }

        [HttpPost("reports")]
        [Authorize]
        public async Task<IActionResult> CreateReport([FromBody] CreateReportDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized("Invalid user ID");
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == dto.OrderId && o.UserId == userId);

            if (order == null)
            {
                return NotFound("Order not found or you don't have permission to report this order");
            }

            var report = new OrderReport
            {
                OrderId = dto.OrderId,
                UserId = userId,
                Content = dto.Content,
                CreatedAt = DateTime.UtcNow,
                Status = "pending"
            };

            _context.OrderReports.Add(report);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Báo cáo đã được gửi thành công" });
        }

        // Endpoint để admin lấy danh sách báo cáo
        [HttpGet("reports")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<object>>> GetReports()
        {
            var reports = await _context.OrderReports
                .Include(r => r.Order)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.ReportId,
                    r.OrderId,
                    r.Content,
                    r.CreatedAt,
                    r.Status,
                    UserName = r.User.Name,
                    OrderDate = r.Order.OrderDate,
                    OrderStatus = r.Order.OrderStatus
                })
                .ToListAsync();

            return Ok(reports);
        }

        // Endpoint để admin cập nhật trạng thái báo cáo
        [HttpPut("reports/{reportId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateReportStatus(int reportId, [FromBody] UpdateReportStatusDto dto)
        {
            var report = await _context.OrderReports.FindAsync(reportId);
            if (report == null)
            {
                return NotFound("Report not found");
            }

            report.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Trạng thái báo cáo đã được cập nhật" });
        }

        public class CreateReportDto
        {
            public int OrderId { get; set; }
            public string Content { get; set; }
        }

        public class UpdateReportStatusDto
        {
            public string Status { get; set; }
        }

        [HttpPost("create-custom-order")]
        [Authorize(Roles = "Seller")]
        public async Task<IActionResult> CreateCustomOrder([FromForm] CreateCustomOrderDto dto)
        {
            try
            {
                var sellerId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                if (sellerId != dto.SellerId)
                {
                    return Forbid("You are not authorized to create orders for this seller");
                }

                // Kiểm tra category
                var category = await _context.Categories.FindAsync(dto.CategoryId);
                if (category == null)
                {
                    return BadRequest(new { success = false, message = "Danh mục không tồn tại" });
                }

                // Xử lý ảnh
                string imageUrl = null;
                if (dto.Image != null)
                {
                    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(dto.Image.FileName);
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "flowers");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var filePath = Path.Combine(uploadsFolder, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await dto.Image.CopyToAsync(stream);
                    }
                    imageUrl = $"/uploads/flowers/{fileName}";
                }

                // Tạo flower với IsCustomOrder và IsHidden
                var flower = new Flower
                {
                    FlowerName = dto.FlowerName,
                    Price = dto.Price,
                    ImageUrl = imageUrl,
                    UserId = sellerId,
                    Quantity = dto.Quantity,
                    ListingDate = DateTime.UtcNow,
                    Status = "Available",
                    Condition = dto.Condition,
                    CategoryId = dto.CategoryId,
                    IsCustomOrder = true,
                    IsVisible = true
                };

                _context.Flowers.Add(flower);
                await _context.SaveChangesAsync();

                // Thêm vào giỏ hàng người mua
                var cart = await GetOrCreateCart(dto.BuyerId);
                var cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    FlowerId = flower.FlowerId,
                    Quantity = dto.Quantity,
                    Price = dto.Price,
                    IsCustomOrder = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.CartItems.Add(cartItem);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Đã tạo sản phẩm tùy chỉnh và thêm vào giỏ hàng",
                    data = new { flowerId = flower.FlowerId }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating custom order");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Có lỗi xảy ra khi tạo sản phẩm tùy chỉnh",
                    error = ex.Message
                });
            }
        }

        private async Task<Cart> GetOrCreateCart(int userId)
        {
            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = "Active"
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }
        private async Task<int> GetOrCreateCartId(int userId)
        {
            var cart = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Status = "Active"
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart.CartId;
        }
        public class CreateCustomOrderDto
        {
            [Required]
            public int BuyerId { get; set; }

            [Required]
            public int SellerId { get; set; }

            [Required]
            [StringLength(100)]
            public string FlowerName { get; set; }

            [Required]
            [Range(0, double.MaxValue)]
            public decimal Price { get; set; }

            [Required]
            [Range(1, int.MaxValue)]
            public int Quantity { get; set; }

            [Required]
            public string Condition { get; set; }

            [Required]
            public int CategoryId { get; set; }

            public IFormFile Image { get; set; }
        }
    }
}