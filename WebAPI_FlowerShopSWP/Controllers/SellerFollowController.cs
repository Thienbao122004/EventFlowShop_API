using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebAPI_FlowerShopSWP.Models;
using WebAPI_FlowerShopSWP.Services;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    public class SellerFollowController : ControllerBase
    {
        private readonly ISellerFollowService _sellerFollowService;

        public SellerFollowController(ISellerFollowService sellerFollowService)
        {
            _sellerFollowService = sellerFollowService;
        }

        [HttpPost("follow")]
        public async Task<IActionResult> FollowSeller([FromBody] SellerFollow model)
        {
            var result = await _sellerFollowService.FollowSeller(model.UserId, model.SellerId);
            return Ok(result);
        }

        [HttpPost("unfollow")]
        public async Task<IActionResult> UnfollowSeller([FromBody] SellerFollow model)
        {
            var result = await _sellerFollowService.UnfollowSeller(model.UserId, model.SellerId);
            return Ok(result);
        }

        [HttpGet("is-following")]
        public async Task<IActionResult> IsFollowing(int userId, int sellerId)
        {
            var result = await _sellerFollowService.IsFollowing(userId, sellerId);
            return Ok(result);
        }

        [HttpGet("followed-sellers/{userId}")]
        public async Task<IActionResult> GetFollowedSellers(int userId)
        {
            var sellers = await _sellerFollowService.GetFollowedSellers(userId);
            return Ok(sellers);
        }
    }
}

