using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FlowersController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<FlowersController> _logger;

        public FlowersController(FlowerEventShopsContext context, IWebHostEnvironment webHostEnvironment, ILogger<FlowersController> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }
        // GET: api/Flowers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Flower>>> GetFlowers()
        {
            return await _context.Flowers.ToListAsync();
        }

        [HttpGet("searchbyname")]
        public async Task<ActionResult<IEnumerable<Flower>>> SearchFlowers(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return BadRequest("Search name cannot be empty.");
            }

            var flowers = await _context.Flowers
                .Where(f => f.FlowerName.ToLower().Contains(name.ToLower()))
                .ToListAsync();

            if (!flowers.Any())
            {
                return NotFound("No flowers found with the given name.");
            }

            return flowers;
        }

        // GET: api/Flowers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Flower>> GetFlower(int id)
        {
            var flower = await _context.Flowers.FindAsync(id);

            if (flower == null)
            {
                return NotFound();
            }

            return flower;
        }

        // PUT: api/Flowers/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFlower(int id, Flower flower)
        {
            if (id != flower.FlowerId)
            {
                return BadRequest();
            }

            _context.Entry(flower).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FlowerExists(id))
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

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Flower>> PostFlower(
     [FromForm] string FlowerName,
     [FromForm] decimal Price,
     [FromForm] int Quantity,
     [FromForm] int CategoryId,
     [FromForm] IFormFile? image)
        {
            try
            {
                int userId;
                try
                {
                    userId = GetCurrentUserId();
                }
                catch (UnauthorizedAccessException)
                {
                    return Unauthorized("User not authenticated or session expired");
                }

                var flower = new Flower
                {
                    FlowerName = FlowerName,
                    Price = Price,
                    Quantity = Quantity,
                    CategoryId = CategoryId,
                    UserId = userId,
                    Condition = "New",
                    Status = "Available",
                    ListingDate = DateTime.UtcNow
                };

                _logger.LogInformation($"Received flower data: {JsonSerializer.Serialize(flower)}");

                if (image != null)
                {
                    flower.ImageUrl = await SaveImageAsync(image);
                }

                _context.Flowers.Add(flower);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetFlower", new { id = flower.FlowerId }, flower);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in PostFlower: {ex}");
                return StatusCode(500, new { message = $"Internal server error: {ex.Message}", details = ex.ToString() });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            _logger.LogWarning("User ID not found in claims");
            throw new UnauthorizedAccessException("User not authenticated or session expired");
        }

        private async Task<string> SaveImageAsync(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                throw new ArgumentException("Invalid file");
            }

            var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "images");
            if (!Directory.Exists(uploadPath))
            {
                Directory.CreateDirectory(uploadPath);
            }

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var filePath = Path.Combine(uploadPath, fileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            return "/images/" + fileName;
        }

        // DELETE: api/Flowers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFlower(int id)
        {
            var flower = await _context.Flowers.FindAsync(id);
            if (flower == null)
            {
                return NotFound();
            }

            _context.Flowers.Remove(flower);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool FlowerExists(int id)
        {
            return _context.Flowers.Any(e => e.FlowerId == id);
        }

        [HttpPost("{flowerId}/reviews")]
        public async Task<ActionResult<Review>> PostReview(int flowerId, [FromBody] Review review)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var flower = await _context.Flowers.FindAsync(flowerId);
            if (flower == null)
            {
                return NotFound("Flower not found.");
            }

            review.FlowerId = flowerId;
            review.ReviewDate = DateTime.UtcNow;

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetReview", new { id = review.ReviewId }, review);
        }

        [HttpGet("{flowerId}/reviews")]
        public async Task<ActionResult<IEnumerable<Review>>> GetReviews(int flowerId)
        {
            var reviews = await _context.Reviews.Where(r => r.FlowerId == flowerId).ToListAsync();
            if (!reviews.Any())
            {
                return NotFound("No reviews found for this flower.");
            }

            return reviews;
        }

        [HttpGet("{flowerId}/canReview")]
        public async Task<ActionResult<bool>> CanReview(int flowerId)
        {
            int userId = GetCurrentUserId();
            var hasPurchased = await _context.Orders
                .AnyAsync(o => o.UserId == userId && o.OrderItems.Any(oi => oi.FlowerId == flowerId));

            return Ok(hasPurchased);
        }
    }
}