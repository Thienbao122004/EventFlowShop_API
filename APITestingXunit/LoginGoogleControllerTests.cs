using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebAPI_FlowerShopSWP.Controllers;
using WebAPI_FlowerShopSWP.Models;
using System.Linq.Expressions;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace APITestingXunit
{
    public class LoginGoogleControllerTests
    {
        private readonly Mock<FlowerEventShopsContext> _mockContext;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<ILogger<LoginGoogleController>> _mockLogger;
        private readonly LoginGoogleController _controller;

        public LoginGoogleControllerTests()
        {
            _mockContext = new Mock<FlowerEventShopsContext>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<LoginGoogleController>>();
            _controller = new LoginGoogleController(_mockContext.Object, _mockConfiguration.Object, _mockLogger.Object);
        }



        [Fact]
        public async Task GoogleLogin_InvalidIdToken_ReturnsServerError()
        {
            // Arrange
            var request = new LoginGoogleController.GoogleLoginRequest
            {
                AccessToken = "valid_access_token",
                
            };

            // Mock the necessary context and configuration behavior
            // ... (mock user retrieval and Google token validation to throw an exception)

            // Act
            var result = await _controller.GoogleLogin(request);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
    }
}
