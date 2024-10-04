using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Web;
using WebAPI_FlowerShopSWP.DTO;
using WebAPI_FlowerShopSWP.Models;
using WebAPI_FlowerShopSWP.Helpers;
using WebAPI_FlowerShopSWP.Configurations;
using Microsoft.Extensions.Options;

namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;
        private readonly VNPayConfig _vnpayConfig;
        private readonly ILogger<PaymentsController> _logger;


        public PaymentsController(
            FlowerEventShopsContext context,
            IOptions<VNPayConfig> vnpayConfig,
            ILogger<PaymentsController> logger)
        {
            _context = context;
            _vnpayConfig = vnpayConfig.Value;
            _logger = logger;
        }

        // GET: api/Payments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
        {
            return await _context.Payments.AsNoTracking().ToListAsync();
        }

        // GET: api/Payments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Payment>> GetPayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
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
                return BadRequest("Payment ID mismatch");
            }

            _context.Entry(payment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!PaymentExists(id))
                {
                    return NotFound();
                }
                throw;
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
                PaymentDate = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetPayment), new { id = payment.PaymentId }, payment);
        }

        // DELETE: api/Payments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePayment(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }

        // VNPay Payment
        [HttpPost("createVnpPayment")]
        public IActionResult CreateVnpPayment([FromBody] PaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (request.Amount <= 0)
            {
                return BadRequest("Amount must be greater than 0.");
            }

            var vnpay = new VnPayLibrary();
            var vnp_Returnurl = "http://localhost:5173/payment-result";
            var vnp_TxnRef = DateTime.Now.Ticks.ToString();
            var vnp_OrderInfo = "order";
            var vnp_OrderType = "other";
            var vnp_Amount = request.Amount * 100;
            var vnp_Locale = "vn";
            var vnp_IpAddr = HttpContext.Connection.RemoteIpAddress?.ToString();
            string vnp_Url = _vnpayConfig.Url;
            string vnp_HashSecret = _vnpayConfig.HashSecret;
            _logger.LogInformation("Creating VNPay payment with parameters: Amount={Amount}, TxnRef={TxnRef}, OrderInfo={OrderInfo}", vnp_Amount, vnp_TxnRef, vnp_OrderInfo);

            vnpay.AddRequestData("vnp_Amount", vnp_Amount.ToString());
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", "127.0.0.1");
            vnpay.AddRequestData("vnp_Locale", vnp_Locale);
            vnpay.AddRequestData("vnp_OrderInfo", vnp_OrderInfo);
            vnpay.AddRequestData("vnp_OrderType", vnp_OrderType);
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TmnCode", _vnpayConfig.TmnCode);
            vnpay.AddRequestData("vnp_TxnRef", vnp_TxnRef);
            vnpay.AddRequestData("vnp_Version", "2.1.0");
            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            _logger.LogInformation("Generated payment URL: {PaymentUrl}", paymentUrl);
            return Ok(new { paymentUrl });
        }

        [HttpGet("vnpay-return")]
        public IActionResult VnPayReturn()
        {
            var vnpay = new VnPayLibrary();

            foreach (var (key, value) in Request.Query)
            {
                vnpay.AddResponseData(key, value.ToString());
            }

            var vnp_SecureHash = Request.Query["vnp_SecureHash"].ToString();
            var orderInfo = vnpay.GetResponseData("vnp_OrderInfo");
            var vnp_TransactionId = vnpay.GetResponseData("vnp_TransactionNo");
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");

            // Ghi log các tham số nhận được
            _logger.LogInformation("Received Response: OrderInfo={OrderInfo}, TransactionId={TransactionId}, ResponseCode={ResponseCode}, SecureHash={SecureHash}", orderInfo, vnp_TransactionId, vnp_ResponseCode, vnp_SecureHash);

            var checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _vnpayConfig.HashSecret);

            if (checkSignature)
            {
                if (vnp_ResponseCode == "00")
                {
                        return Ok("Thanh toán thành công");
                }
                else
                {
                    return BadRequest($"Thanh toán không thành công. Mã lỗi: {vnp_ResponseCode}");
                }
            }
            else
            {
                return BadRequest("Invalid signature");
            }
        }
    }

    public class PaymentRequest
    {

        public decimal Amount { get; set; }
    }
}