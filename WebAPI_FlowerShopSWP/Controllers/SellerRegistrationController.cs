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

namespace WebAPI_FlowerShopSWP.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SellerRegistrationController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;
        private readonly ILogger<SellerRegistrationController> _logger;

        public SellerRegistrationController(FlowerEventShopsContext context, ILogger<SellerRegistrationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<SellerRegistrationRequestDto>> CreateRequest(SellerRegistrationRequestDto requestDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null || user.UserType != "Buyer")
            {
                return BadRequest("Only buyers can register to become sellers.");
            }

            var existingRequest = await _context.SellerRegistrationRequests
                .FirstOrDefaultAsync(r => r.UserId == userId && r.Status == "Pending");
            if (existingRequest != null)
            {
                return BadRequest("You already have a pending seller registration request.");
            }

            var request = new SellerRegistrationRequest
            {
                UserId = userId,
                StoreName = requestDto.StoreName,
                Email = user.Email,
                Address = requestDto.Address,
                Phone = requestDto.Phone,
                IdCard = requestDto.IdCard,
                Status = "Pending",
                RequestDate = DateTime.UtcNow
            };

            _context.SellerRegistrationRequests.Add(request);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"New seller registration request created for user {userId}");

            return CreatedAtAction(nameof(GetRequest), new { id = request.RequestId }, requestDto);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ProcessRequest(int id, [FromBody] SellerRegistrationApproval approval)
        {
            var request = await _context.SellerRegistrationRequests.FindAsync(id);
            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != "Pending")
            {
                return BadRequest("This request has already been processed.");
            }

            request.Status = approval.Approved ? "Approved" : "Rejected";
            request.ProcessedDate = DateTime.UtcNow;

            if (approval.Approved)
            {
                var user = await _context.Users.FindAsync(request.UserId);
                if (user != null)
                {
                    user.UserType = "Seller";
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RequestExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            _logger.LogInformation($"Seller registration request {id} processed. Status: {request.Status}");

            return NoContent();
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<SellerRegistrationRequest>> GetRequest(int id)
        {
            var request = await _context.SellerRegistrationRequests.FindAsync(id);

            if (request == null)
            {
                return NotFound();
            }

            return request;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<SellerRegistrationRequest>>> GetPendingRequests()
        {
            return await _context.SellerRegistrationRequests
                .Where(r => r.Status == "Pending")
                .ToListAsync();
        }

        private bool RequestExists(int id)
        {
            return _context.SellerRegistrationRequests.Any(e => e.RequestId == id);
        }
    }

    public class SellerRegistrationRequestDto
    {
        [Required]
        [StringLength(255)]
        public string StoreName { get; set; }

        [Required]
        [StringLength(255)]
        public string Address { get; set; }

        [Required]
        [StringLength(20)]
        [Phone]
        public string Phone { get; set; }

        [Required]
        [StringLength(12)]
        [RegularExpression(@"^\d{9,12}$", ErrorMessage = "IdCard must be 9 to 12 digits.")]
        public string IdCard { get; set; }
    }

    public class SellerRegistrationApproval
    {
        public bool Approved { get; set; }
    }
}