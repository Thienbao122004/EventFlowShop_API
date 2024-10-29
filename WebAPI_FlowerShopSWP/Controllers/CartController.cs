using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebAPI_FlowerShopSWP.DTO;
using WebAPI_FlowerShopSWP.Models;
using Microsoft.AspNetCore.Authorization;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CartController> _logger;

        public CartController(FlowerEventShopsContext context, IMapper mapper, ILogger<CartController> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<CartDto>> GetUserCart()
        {
            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Flower)
                            .ThenInclude(f => f.Seller)
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

                if (cart == null)
                {
                    cart = new Cart
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Status = "Active",
                        CartItems = new List<CartItem>()
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                var cartDto = new CartDto
                {
                    CartId = cart.CartId,
                    UserId = cart.UserId,
                    Status = cart.Status,
                    Items = cart.CartItems
                        .Where(ci => ci.Flower != null)
                        .Select(item => new CartItemDto
                        {
                            CartItemId = item.CartItemId,
                            FlowerId = item.FlowerId,
                            FlowerName = item.Flower.FlowerName,
                            Price = item.Price,
                            Quantity = item.Quantity,
                            ImageUrl = item.Flower.ImageUrl,
                            IsCustomOrder = item.IsCustomOrder,
                            SellerFullName = item.Flower.Seller?.FullName ?? "Unknown Seller"
                        }).ToList(),
                    TotalAmount = cart.CartItems.Sum(item => item.Price * item.Quantity)
                };

                return Ok(cartDto);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error getting cart: {ex}");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("add-item")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartDto dto)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                if (currentUserId <= 0)
                {
                    return BadRequest(new { success = false, message = "Invalid user ID" });
                }

                var flower = await _context.Flowers
                    .Include(f => f.Seller)
                    .FirstOrDefaultAsync(f => f.FlowerId == dto.FlowerId);

                if (flower == null)
                {
                    return BadRequest(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                // Xử lý đơn hàng tùy chỉnh
                if (dto.IsCustomOrder)
                {
                    // Kiểm tra người dùng hiện tại phải là seller
                    if (currentUserId == flower.UserId)
                    {
                        // Tìm giỏ hàng của buyer
                        var buyerCart = await _context.Carts
                            .Include(c => c.CartItems) // Include CartItems để kiểm tra trùng lặp
                            .FirstOrDefaultAsync(c => c.UserId == dto.BuyerId && c.Status == "Active");

                        if (buyerCart == null)
                        {
                            buyerCart = new Cart
                            {
                                UserId = dto.BuyerId,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow,
                                Status = "Active"
                            };
                            _context.Carts.Add(buyerCart);
                            await _context.SaveChangesAsync();
                        }

                        // Kiểm tra xem sản phẩm custom đã tồn tại trong giỏ hàng chưa
                        var existingCustomItem = await _context.CartItems
                            .FirstOrDefaultAsync(ci =>
                                ci.CartId == buyerCart.CartId &&
                                ci.FlowerId == dto.FlowerId &&
                                ci.IsCustomOrder &&
                                ci.Price == dto.Price); // Thêm điều kiện về giá để phân biệt các custom order khác nhau

                        if (existingCustomItem != null)
                        {
                            existingCustomItem.UpdatedAt = DateTime.UtcNow;
                        }

                        else
                        {
                            // Nếu chưa tồn tại, tạo mới
                            var cartItem = new CartItem
                            {
                                CartId = buyerCart.CartId,
                                FlowerId = dto.FlowerId,
                                Quantity = dto.Quantity,
                                Price = dto.Price,
                                IsCustomOrder = true,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.CartItems.Add(cartItem);
                        }

                        await _context.SaveChangesAsync();

                        return Ok(new
                        {
                            success = true,
                            message = "Đã thêm đơn hàng tùy chỉnh vào giỏ hàng của người mua",
                            cartId = buyerCart.CartId
                        });
                    }
                    else
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "Chỉ người bán mới có thể thêm đơn hàng tùy chỉnh"
                        });
                    }
                }

                // Xử lý sản phẩm thông thường
                if (flower.Quantity < dto.Quantity)
                {
                    return BadRequest(new { success = false, message = "Số lượng sản phẩm không đủ" });
                }

                var userCart = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == currentUserId && c.Status == "Active");

                if (userCart == null)
                {
                    userCart = new Cart
                    {
                        UserId = currentUserId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Status = "Active"
                    };
                    _context.Carts.Add(userCart);
                    await _context.SaveChangesAsync();
                }

                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == userCart.CartId &&
                                             ci.FlowerId == dto.FlowerId &&
                                             !ci.IsCustomOrder);

                if (existingItem != null)
                {
                    existingItem.Quantity += dto.Quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var newItem = new CartItem
                    {
                        CartId = userCart.CartId,
                        FlowerId = dto.FlowerId,
                        Quantity = dto.Quantity,
                        Price = dto.Price,
                        IsCustomOrder = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.CartItems.Add(newItem);
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Đã thêm vào giỏ hàng thành công",
                    cartId = userCart.CartId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error adding to cart: {ex}");
                return BadRequest(new { success = false, message = $"Không thể thêm vào giỏ hàng: {ex.Message}" });
            }
        }

        [HttpPut("update-quantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateCartItemDto dto)
        {
            try
            {
                var cartItem = await _context.CartItems
                    .Include(ci => ci.Flower)
                    .FirstOrDefaultAsync(ci => ci.CartItemId == dto.CartItemId);

                if (cartItem == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                if (!cartItem.IsCustomOrder && cartItem.Flower.Quantity < dto.Quantity)
                {
                    return BadRequest(new { success = false, message = "Số lượng sản phẩm không đủ" });
                }

                cartItem.Quantity = dto.Quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Đã cập nhật số lượng" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating cart item quantity: {ex}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpDelete("remove-item/{cartItemId}")]
        public async Task<IActionResult> RemoveFromCart(int cartItemId)
        {
            try
            {
                var cartItem = await _context.CartItems.FindAsync(cartItemId);
                if (cartItem == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "Đã xóa sản phẩm khỏi giỏ hàng" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error removing cart item: {ex}");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }
    }
}