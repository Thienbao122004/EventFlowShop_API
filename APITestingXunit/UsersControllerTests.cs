using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using System.Text;
using System.Threading.Tasks;
using WebAPI_FlowerShopSWP;
using System.Net.Http;

namespace APITestingXunit
{
    public class UsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public UsersControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task TestGetAllUsers()
        {
            var response = await _client.GetAsync("/api/Users");
            response.EnsureSuccessStatusCode();
            var responseString = await response.Content.ReadAsStringAsync();
            Assert.NotNull(responseString); // Kiểm tra dữ liệu không rỗng
        }

        [Fact]
        public async Task TestRegisterUser()
        {
            var newUser = new
            {
                Name = "chophuc2",
                Email = $"chophuc2@gmail.com",
                Password = "12345"
            };

            var content = new StringContent(JsonConvert.SerializeObject(newUser), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/Users/register", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("userId", responseString); // Kiểm tra trả về chứa UserId
        }

        [Fact]
        public async Task TestUserLogin()
        {
            var loginUser = new
            {
                Name = "nguyenthang",
                Password = "12345"
            };

            var content = new StringContent(JsonConvert.SerializeObject(loginUser), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("/api/Users/login", content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Assert.Contains("token", responseString); // Kiểm tra kết quả có chứa token
        }

        [Fact]
        public async Task TestDeleteUser()
        {
            var response = await _client.DeleteAsync("/api/Users/30"); // ID người dùng cần xóa
            Assert.Equal(System.Net.HttpStatusCode.NoContent, response.StatusCode); // Kiểm tra mã 204 - No Content
        }
    }
}
