namespace WebAPI_FlowerShopSWP.Repository
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string email, string subject, string message); //hàm gửi email
    }
}
