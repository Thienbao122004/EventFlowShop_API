using WebAPI_FlowerShopSWP.Models;
using WebAPI_FlowerShopSWP.DTO;
namespace WebAPI_FlowerShopSWP.Services
{
    public interface ICartService
    {
        Task<Cart> GetUserCartAsync(int userId);
        Task<CartItem> AddToCartAsync(int userId, AddToCartDto dto);
        Task<CartItem> UpdateQuantityAsync(int cartItemId, int quantity);
        Task RemoveFromCartAsync(int cartItemId);
        Task<IEnumerable<CartItemDto>> GetCartItemsAsync(int userId);
    }
}
