using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using WebAPI_FlowerShopSWP.Models;
using WebAPI_FlowerShopSWP.Helpers;
using WebAPI_FlowerShopSWP.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity.UI.Services;
using WebAPI_FlowerShopSWP.Repository;
using IEmailSender = WebAPI_FlowerShopSWP.Repository.IEmailSender;
using System.Globalization;
namespace WebAPI_FlowerShopSWP.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ControllerBase
    {
        private readonly FlowerEventShopsContext _context;
        private readonly VNPayConfig _vnpayConfig;
        private readonly ILogger<PaymentsController> _logger;
        private readonly IEmailSender _emailSender;

        public PaymentsController(
            FlowerEventShopsContext context,
            IOptions<VNPayConfig> vnpayConfig,
            ILogger<PaymentsController> logger,
            IEmailSender emailSender)
        {
            _context = context;
            _vnpayConfig = vnpayConfig.Value;
            _logger = logger;
            _emailSender = emailSender;
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.PaymentId == id);
        }

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
            var vnp_TxnRef = DateTime.Now.Ticks.ToString(); // Unique transaction reference
            var vnp_OrderInfo = "order";
            var vnp_OrderType = "other";
            var vnp_Amount = request.Amount * 100;
            var vnp_Locale = "vn";
            var vnp_IpAddr = HttpContext.Connection.RemoteIpAddress?.ToString();

            string vnp_Url = _vnpayConfig.Url;
            string vnp_HashSecret = _vnpayConfig.HashSecret;

            _logger.LogInformation("Creating VNPay payment with parameters: Amount={Amount}, TxnRef={TxnRef}, OrderInfo={OrderInfo}", vnp_Amount, vnp_TxnRef, vnp_OrderInfo);

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", _vnpayConfig.TmnCode);
            vnpay.AddRequestData("vnp_Amount", vnp_Amount.ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", vnp_IpAddr);
            vnpay.AddRequestData("vnp_Locale", vnp_Locale);
            vnpay.AddRequestData("vnp_OrderInfo", vnp_OrderInfo);
            vnpay.AddRequestData("vnp_OrderType", vnp_OrderType);
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_Returnurl);
            vnpay.AddRequestData("vnp_TxnRef", vnp_TxnRef);

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);
            _logger.LogInformation("Generated payment URL: {PaymentUrl}", paymentUrl);
            return Ok(new { paymentUrl });
        }
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturn()
        {
            var vnpay = new VnPayLibrary();

            foreach (var (key, value) in Request.Query)
            {
                vnpay.AddResponseData(key, value.ToString());
            }

            var vnp_SecureHash = Request.Query["vnp_SecureHash"].ToString();
            var vnp_TransactionId = vnpay.GetResponseData("vnp_TransactionNo");
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_TxnRef = vnpay.GetResponseData("vnp_TxnRef");
            var vnp_Amount = vnpay.GetResponseData("vnp_Amount");

            _logger.LogInformation("Received VNPay Response: TxnRef={TxnRef}, TransactionId={TransactionId}, ResponseCode={ResponseCode}, Amount={Amount}",
                vnp_TxnRef, vnp_TransactionId, vnp_ResponseCode, vnp_Amount);

            var checkSignature = vnpay.ValidateSignature(vnp_SecureHash, _vnpayConfig.HashSecret);

            if (!checkSignature)
            {
                _logger.LogWarning("Invalid VNPay signature");
                return BadRequest(new { status = "error", message = "Invalid signature" });
            }

            if (string.IsNullOrEmpty(vnp_TxnRef))
            {
                _logger.LogError("TxnRef is null or empty");
                return BadRequest(new { status = "error", message = "Transaction reference is missing." });
            }

            DateTime txnDateTime;
            if (!long.TryParse(vnp_TxnRef, out long ticks))
            {
                _logger.LogError("Invalid TxnRef format: {TxnRef}", vnp_TxnRef);
                return BadRequest(new { status = "error", message = "Invalid transaction reference format." });
            }

            txnDateTime = new DateTime(ticks);
            _logger.LogInformation("Parsed TxnRef DateTime: {TxnDateTime}", txnDateTime);

            // Tìm đơn hàng trong khoảng thời gian hợp lý
            var order = await _context.Orders
                .Where(o => o.OrderDate >= txnDateTime.AddMinutes(-5) && o.OrderDate <= txnDateTime.AddMinutes(5))
                .OrderByDescending(o => o.OrderDate)
                .FirstOrDefaultAsync();

            if (order == null)
            {
                _logger.LogError("No matching order found for TxnRef: {TxnRef}", vnp_TxnRef);
                return BadRequest(new { status = "error", message = "No matching order found." });
            }

            if (vnp_ResponseCode == "00")
            {
                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    Amount = decimal.Parse(vnp_Amount) / 100,
                    PaymentDate = DateTime.Now,
                    PaymentStatus = "Success"
                };

                try
                {
                    _context.Payments.Add(payment);
                    order.OrderStatus = "Completed";
                    await _context.SaveChangesAsync();

                    var user = await _context.Users.FindAsync(order.UserId);
                    if (user != null)
                    {
                        await SendConfirmationEmail(user, order, payment.Amount);
                    }

                    return Ok(new { status = "success", message = "Thanh toán thành công" });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing payment");
                    return StatusCode(500, new { status = "error", message = "Error processing payment." });
                }
            }
            else
            {
                // Không thay đổi trạng thái đơn hàng nếu thanh toán thất bại
                // Thay vào đó, chỉ thêm một bản ghi thanh toán thất bại
                var failedPayment = new Payment
                {
                    OrderId = order.OrderId,
                    Amount = decimal.Parse(vnp_Amount) / 100,
                    PaymentDate = DateTime.Now,
                    PaymentStatus = "Failed"
                };

                try
                {
                    _context.Payments.Add(failedPayment);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error recording failed payment");
                }

                return Ok(new { status = "failed", message = $"Thanh toán không thành công. Mã lỗi: {vnp_ResponseCode}" });
            }
        }
        private async Task SendConfirmationEmail(User user, Order order, decimal totalAmount)
        {
            _logger.LogInformation("Preparing to send confirmation email to {UserEmail}", user.Email);

            string emailSubject = "Xác nhận đơn hàng và thanh toán";
            string emailBody = $@"
Xin chào {user.Name},

Đơn hàng của bạn đã được xác nhận và thanh toán thành công.

Chi tiết đơn hàng:
- Mã đơn hàng: {order.OrderId}
- Ngày đặt hàng: {order.OrderDate:dd/MM/yyyy HH:mm}
- Tổng giá trị: {totalAmount:N0} VNĐ

Cảm ơn bạn đã mua hàng tại cửa hàng chúng tôi!

Trân trọng,
Đội ngũ hỗ trợ khách hàng";

            try
            {
                await _emailSender.SendEmailAsync(user.Email, emailSubject, emailBody);
                _logger.LogInformation("Confirmation email sent successfully to {UserEmail}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email to {UserEmail}. Error: {ErrorMessage}", user.Email, ex.Message);
            }
        }

        public class PaymentRequest
        {
            public decimal Amount { get; set; }
        }
    }
}
