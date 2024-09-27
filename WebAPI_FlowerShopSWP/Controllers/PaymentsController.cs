using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI_FlowerShopSWP.DTO;
using WebAPI_FlowerShopSWP.Models;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;

        public PaymentsController(FlowerEventShopsContext context)
        {
            _context = context;
        }

        // GET: api/Payments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
        {
            // Sử dụng AsNoTracking để cải thiện hiệu suất khi dữ liệu không cần được thay đổi
            return await _context.Payments.AsNoTracking().ToListAsync();
        }

        // GET: api/Payments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPayment(int id)
        {
            // Sử dụng FindAsync để tìm kiếm một thực thể cụ thể theo khóa chính
            var payment = await _context.Payments.FindAsync(id);

            if (payment == null)
            {
                // Trả về NotFound nếu không tìm thấy thanh toán
                return NotFound();
            }

            return Ok(payment);
        }

        // PUT: api/Payments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutPayment(int id, Payment payment)
        {
            if (id != payment.PaymentId)
            {
                // Trả về BadRequest nếu id không khớp với PaymentId
                return BadRequest("Payment ID mismatch");
            }

            // Đánh dấu thực thể này là đã sửa đổi
            _context.Entry(payment).State = EntityState.Modified;

            try
            {
                // Lưu thay đổi vào cơ sở dữ liệu
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Kiểm tra nếu payment vẫn tồn tại
                if (!PaymentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw; // Ném lại ngoại lệ nếu có vấn đề về cạnh tranh dữ liệu
                }
            }

            return NoContent();
        }

        // POST: api/Payments
        [HttpPost]
        public async Task<ActionResult<Payment>> PostPayment(PaymentDto paymentDto)
        {
            var payment = new Payment
            {
                OrderId = paymentDto.OrderId,
                Amount = paymentDto.Amount,
                PaymentStatus = paymentDto.PaymentStatus,
                PaymentDate = DateTime.Now // hoặc mặc định từ SQL
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPayment), new { id = payment.PaymentId }, payment);
        }


        // DELETE: api/Payments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(int id)
        {
            // Tìm kiếm thanh toán theo ID
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                // Trả về NotFound nếu không tìm thấy thanh toán
                return NotFound();
            }

            // Xóa thanh toán và lưu thay đổi
            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Kiểm tra xem thanh toán có tồn tại không
        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }
    }
}
