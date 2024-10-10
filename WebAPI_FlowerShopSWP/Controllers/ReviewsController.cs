﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;

        public ReviewsController(FlowerEventShopsContext context)
        {
            _context = context;
        }

        // GET: api/Reviews
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviews()
        {
            return await _context.Reviews.ToListAsync();
        }

        // GET: api/Reviews/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Review>> GetReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);

            if (review == null)
            {
                return NotFound();
            }

            return review;
        }

        [HttpPost]
        public async Task<ActionResult<object>> PostReview(Review review)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserId == userId && r.FlowerId == review.FlowerId);

            if (existingReview != null)
            {
                return BadRequest("You have already reviewed this product. Please edit your existing review.");
            }

            review.UserId = userId;
            review.ReviewDate = DateTime.Now;
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(review.UserId);
            var reviewWithUserName = new
            {
                review.ReviewId,
                review.UserId,
                UserName = user.FullName,
                review.FlowerId,
                review.Rating,
                review.ReviewComment,
                review.ReviewDate
            };

            return CreatedAtAction("GetReview", new { id = review.ReviewId }, reviewWithUserName);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutReview(int id, Review review)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var existingReview = await _context.Reviews.FindAsync(id);

            if (existingReview == null)
            {
                return NotFound();
            }

            if (existingReview.UserId != userId)
            {
                return Forbid();
            }

            existingReview.Rating = review.Rating;
            existingReview.ReviewComment = review.ReviewComment;
            existingReview.ReviewDate = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ReviewExists(id))
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

        [HttpGet("flower/{flowerId}")]
        public async Task<ActionResult<object>> GetReviewsByFlowerId(int flowerId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.FlowerId == flowerId)
                .Include(r => r.User)
                .OrderByDescending(r => r.ReviewDate)
                .Select(r => new
                {
                    r.ReviewId,
                    r.UserId,
                    UserName = r.User.FullName,
                    r.FlowerId,
                    r.Rating,
                    r.ReviewComment,
                    r.ReviewDate
                })
                .ToListAsync();

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            var result = new
            {
                AverageRating = Math.Round(averageRating, 2),
                Reviews = reviews
            };

            return result;
        }

        private async Task<bool> HasUserPurchasedFlower(int userId, int flowerId)
        {
            return await _context.OrderItems
                .AnyAsync(od => od.Order.UserId == userId && od.FlowerId == flowerId && od.Order.OrderStatus == "Completed");
        }

        [HttpGet("canReview/{flowerId}")]
        public async Task<ActionResult<bool>> CanUserReview(int flowerId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var canReview = await HasUserPurchasedFlower(userId, flowerId);
            return Ok(canReview);
        }

        // DELETE: api/Reviews/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review == null)
            {
                return NotFound();
            }

            _context.Reviews.Remove(review);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ReviewExists(int id)
        {
            return _context.Reviews.Any(e => e.ReviewId == id);
        }
    }
}