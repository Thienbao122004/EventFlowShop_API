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
        public async Task<ActionResult<IEnumerable<object>>> GetFlowers()
        {
            var flowers = await _context.Flowers
                .Include(f => f.Seller)
                .Where(f => f.IsVisible == true && f.IsCustomOrder == false)
                .Select(f => new
                {
                    f.FlowerId,
                    f.FlowerName,
                    f.Price,
                    f.Quantity,
                    f.CategoryId,
                    f.Condition,
                    f.Status,
                    f.ListingDate,
                    f.ImageUrl,
                    SellerName = f.Seller.Name,
                    f.IsCustomOrder,
                    f.IsVisible
                })
                .ToListAsync();

            return Ok(flowers);
        }

        [HttpGet("best-selling")]
        public async Task<ActionResult<IEnumerable<Flower>>> GetBestSellingFlowers()
        {
            var bestSellingFlowers = await _context.OrderItems
                .GroupBy(oi => oi.FlowerId)
                .Select(g => new { FlowerId = g.Key, TotalSold = g.Sum(oi => oi.Quantity) })
                .OrderByDescending(x => x.TotalSold)
                .Take(10)  // Lấy top 10 sản phẩm bán chạy nhất
                .Join(_context.Flowers,
                    bs => bs.FlowerId,
                    f => f.FlowerId,
                    (bs, f) => f)
                .ToListAsync();

            return bestSellingFlowers;
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
        [HttpPut("{id}")]
        public async Task<IActionResult> PutFlower(int id, [FromForm] string FlowerName, [FromForm] decimal Price, [FromForm] int Quantity, [FromForm] string Status, [FromForm] string Category, [FromForm] IFormFile? image)
        {
            var flower = await _context.Flowers.FindAsync(id);
            if (flower == null)
            {
                return NotFound();
            }

            flower.FlowerName = FlowerName;
            flower.Price = Price;
            flower.Quantity = Quantity;
            flower.Status = Status;

            // Find the category by name
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.CategoryName == Category);
            if (category != null)
            {
                flower.CategoryId = category.CategoryId;
            }

            // If there's a new image, update it
            if (image != null)
            {
                flower.ImageUrl = await SaveImageAsync(image);
            }

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
 [FromForm] string Condition,
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
                    Condition = Condition,
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

            var reviews = await _context.Reviews.Where(r => r.FlowerId == id).ToListAsync();
            _context.Reviews.RemoveRange(reviews);

            var orderItems = await _context.OrderItems.Where(oi => oi.FlowerId == id).ToListAsync();
            _context.OrderItems.RemoveRange(orderItems);

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
        // GET: api/Flowers?sellerId={userId}
        //[Authorize]
        [HttpGet("seller/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetFlowersBySeller(int userId)
        {
            var flowers = await _context.Flowers
                .Include(f => f.Seller)
                .Where(f => f.UserId == userId)
                .Select(f => new
                {
                    f.FlowerId,
                    f.FlowerName,
                    f.Price,
                    f.Quantity,
                    f.CategoryId,
                    f.Condition,
                    f.Status,
                    f.ListingDate,
                    f.ImageUrl,
                    SellerName = f.Seller.Name
                })
                .ToListAsync();

            if (!flowers.Any())
            {
                return NotFound("No flowers found for this seller.");
            }

            return Ok(flowers);
        }

        [Authorize]
        [HttpGet("manage/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> ManageProducts(int userId)
        {
            var currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (currentUserId != userId)
            {
                return Forbid(); // Only the seller can manage their products
            }

            var flowers = await _context.Flowers
                .Where(f => f.UserId == userId)
                .ToListAsync();

            return Ok(flowers);
        }


        [HttpGet]
        [Route("categories")]
        public IActionResult GetCategories()
        {

            var categories = _context.Categories
                .Select(c => new { c.CategoryName })
                .ToList();

            return Ok(categories);
        }

        [Authorize]
        [HttpPost("custom-order")]
        public async Task<ActionResult<Flower>> CreateCustomOrder([FromBody] CustomOrderDto dto)
        {
            try
            {
                var userId = GetCurrentUserId();

                var flower = new Flower
                {
                    FlowerName = dto.FlowerName,
                    Price = dto.Price,
                    ImageUrl = dto.ImageUrl,
                    Quantity = 1,
                    CategoryId = dto.CategoryId,
                    UserId = userId,
                    IsVisible = false,
                    IsCustomOrder = true,
                    Status = "Custom Order",
                    Condition = "New",
                    ListingDate = DateTime.UtcNow
                };

                _context.Flowers.Add(flower);
                await _context.SaveChangesAsync();

                // Tự động thêm vào giỏ hàng
                var cartItem = new CartItem
                {
                    CartId = dto.CartId,
                    FlowerId = flower.FlowerId,
                    Quantity = 1,
                    Price = flower.Price,
                    IsCustomOrder = true
                };

                _context.CartItems.Add(cartItem);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created custom order: {JsonSerializer.Serialize(flower)}");

                return Ok(new
                {
                    success = true,
                    message = "Đã tạo đơn hàng tùy chỉnh thành công",
                    data = flower
                });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in CreateCustomOrder: {ex}");
                return BadRequest(new
                {
                    success = false,
                    message = $"Lỗi khi tạo đơn hàng: {ex.Message}"
                });
            }
        }
        [HttpPut("update-visibility")]
        public IActionResult UpdateFlowerVisibility()
        {
            var currentTime = DateTime.UtcNow;
            var flowersToUpdate = _context.Flowers.Where(flower =>
                flower.ListingDate.HasValue &&
                currentTime > flower.ListingDate.Value.AddHours(24) &&
                flower.IsVisible).ToList();

            foreach (var flower in flowersToUpdate)
            {
                flower.IsVisible = false;
            }

            _context.SaveChanges();

            return Ok(new { Message = $"{flowersToUpdate.Count} flowers were updated to not visible." });
        }
    }
}