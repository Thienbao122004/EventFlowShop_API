using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Services
{
    public class SellerFollowService : ISellerFollowService
    {
        private readonly FlowerEventShopsContext _context;

        public SellerFollowService(FlowerEventShopsContext context)
        {
            _context = context;
        }
        public async Task<int> GetFollowersCount(int sellerId)
        {
            return await _context.SellerFollows.CountAsync(sf => sf.SellerId == sellerId);
        }

        public async Task<bool> FollowSeller(int userId, int sellerId)
        {
            var follow = new SellerFollow
            {
                UserId = userId,
                SellerId = sellerId,
                FollowDate = DateTime.UtcNow
            };

            _context.SellerFollows.Add(follow);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> UnfollowSeller(int userId, int sellerId)
        {
            var follow = await _context.SellerFollows
                .FirstOrDefaultAsync(f => f.UserId == userId && f.SellerId == sellerId);

            if (follow == null)
                return false;

            _context.SellerFollows.Remove(follow);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<bool> IsFollowing(int userId, int sellerId)
        {
            return await _context.SellerFollows
                .AnyAsync(f => f.UserId == userId && f.SellerId == sellerId);
        }

        public async Task<IEnumerable<User>> GetFollowedSellers(int userId)
        {
            return await _context.SellerFollows
                .Where(f => f.UserId == userId)
                .Select(f => f.Seller)
                .ToListAsync();
        }
    }
}