using System.Net.Mail;
using System.Net;

namespace WebAPI_FlowerShopSWP.Repository
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true, //bật bảo mật
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("thienbao122004@gmail.com", "ywtbmwletfzotuzs")
            };

            return client.SendMailAsync(
                new MailMessage(from: "thienbao122004@gmail.com",
                                to: email,
                                subject,
                                message
                                ));
        }
    }
}
