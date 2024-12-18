﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WebAPI_FlowerShopSWP.DTO;
using WebAPI_FlowerShopSWP.Models;
using WebAPI_FlowerShopSWP.Services;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    public class SellerFollowController : ControllerBase
    {
        private readonly ISellerFollowService _sellerFollowService;
        private readonly INotificationService _notificationService;
        private readonly IHubContext<NotificationHub> _hubContext;

        public SellerFollowController(
        ISellerFollowService sellerFollowService,
        INotificationService notificationService,
        IHubContext<NotificationHub> hubContext)
        {
            _sellerFollowService = sellerFollowService;
            _notificationService = notificationService;
            _hubContext = hubContext;
        }


        [HttpGet("followers-count/{sellerId}")]
        public async Task<IActionResult> GetFollowersCount(int sellerId)
        {
            var count = await _sellerFollowService.GetFollowersCount(sellerId);
            return Ok(count);
        }

        [HttpPost("follow")]
        public async Task<IActionResult> FollowSeller([FromBody] SellerFollow model)
        {
            var result = await _sellerFollowService.FollowSeller(model.UserId, model.SellerId);

            // Create notification for the seller
            var notification = new CreateNotificationDTO
            {
                UserId = model.SellerId,
                Title = "Người theo dõi mới",
                Content = $"Bạn có một người theo dõi mới!",
                Type = "Follow",
                RelatedId = model.UserId,
                RelatedType = "User"
            };

            var createdNotification = await _notificationService.CreateNotification(notification);

            // Send real-time notification through SignalR
            await _hubContext.Clients.User(model.SellerId.ToString())
                .SendAsync("ReceiveNotification", createdNotification);

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

