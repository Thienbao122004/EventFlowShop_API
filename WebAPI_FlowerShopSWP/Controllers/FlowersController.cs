using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public FlowersController(FlowerEventShopsContext context)
        {
            _context = context;
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

        // POST: api/Flowers
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Flower>> PostFlower(Flower flower)
        {
            _context.Flowers.Add(flower);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetFlower", new { id = flower.FlowerId }, flower);
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
    }
}
