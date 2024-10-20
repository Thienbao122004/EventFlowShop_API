using System.Threading.Tasks;
using System.Collections.Generic;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Services
{
    public interface ISellerFollowService
    {
        Task<bool> FollowSeller(int userId, int sellerId);
        Task<bool> UnfollowSeller(int userId, int sellerId);
        Task<bool> IsFollowing(int userId, int sellerId);
        Task<IEnumerable<User>> GetFollowedSellers(int userId);
        Task<int> GetFollowersCount(int sellerId);
    }
}