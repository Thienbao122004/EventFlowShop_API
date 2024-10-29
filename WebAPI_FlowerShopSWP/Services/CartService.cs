using AutoMapper;
using Microsoft.EntityFrameworkCore;
using WebAPI_FlowerShopSWP.DTO;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Services
{
    public class CartService : ICartService
    {
        private readonly FlowerEventShopsContext _context;
        private readonly IMapper _mapper;

        public CartService(FlowerEventShopsContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<Cart> GetUserCartAsync(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Flower)
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

        public async Task<CartItem> AddToCartAsync(int userId, AddToCartDto dto)
        {
            var cart = await GetUserCartAsync(userId);

            var cartItem = new CartItem
            {
                CartId = cart.CartId,
                FlowerId = dto.FlowerId,
                Quantity = dto.Quantity,
                Price = dto.Price,
                IsCustomOrder = dto.IsCustomOrder,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();

            return cartItem;
        }

        // Thêm phương thức UpdateQuantityAsync
        public async Task<CartItem> UpdateQuantityAsync(int cartItemId, int quantity)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem == null)
            {
                throw new KeyNotFoundException($"CartItem with ID {cartItemId} not found");
            }

            cartItem.Quantity = quantity;
            cartItem.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return cartItem;
        }

        // Thêm phương thức RemoveFromCartAsync
        public async Task RemoveFromCartAsync(int cartItemId)
        {
            var cartItem = await _context.CartItems.FindAsync(cartItemId);
            if (cartItem != null)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
        }

        // Thêm phương thức GetCartItemsAsync
        public async Task<IEnumerable<CartItemDto>> GetCartItemsAsync(int userId)
        {
            var cart = await GetUserCartAsync(userId);
            var cartItems = await _context.CartItems
                .Include(ci => ci.Flower)
                .Where(ci => ci.CartId == cart.CartId)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CartItemDto>>(cartItems);
        }
    }
}