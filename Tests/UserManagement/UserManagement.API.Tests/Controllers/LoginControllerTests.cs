using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using UserManagement.API.Controllers;
using UserManagement.Application.DTOs;
using UserManagement.Domain.Entities;
using Xunit;

namespace UserManagement.API.Tests.Controllers
{
    public class LoginControllerTests
    {
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<SignInManager<User>> _signInManagerMock;
        private readonly IConfiguration _configuration;
        private readonly Mock<ILogger<LoginController>> _loggerMock;
        private readonly Mock<ProblemDetailsFactory> _problemDetailsFactoryMock;
        private readonly LoginController _loginController;

        public LoginControllerTests()
        {
            _userManagerMock = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
            _signInManagerMock = new Mock<SignInManager<User>>(
            _userManagerMock.Object, Mock.Of<IHttpContextAccessor>(), Mock.Of<IUserClaimsPrincipalFactory<User>>(), null, null, null, null);

            // Load configuration from appsettings.json
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
            _configuration = configurationBuilder;

            _loggerMock = new Mock<ILogger<LoginController>>();
            _problemDetailsFactoryMock = new Mock<ProblemDetailsFactory>();

            _loginController = new LoginController(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _configuration,
                _loggerMock.Object,
                _problemDetailsFactoryMock.Object);
        }

        [Fact]
        public async Task Authenticate_ValidCredentials_ReturnsOkResult()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "user@example.com", Password = "password" };
            var user = new User { Id = Guid.NewGuid(), Email = "user@example.com" };
            _userManagerMock.Setup(manager => manager.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(manager => manager.IsEmailConfirmedAsync(user))
                .ReturnsAsync(true);
            _signInManagerMock.Setup(manager => manager.CheckPasswordSignInAsync(user, loginDto.Password, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _signInManagerMock.Setup(manager => manager.SignInAsync(user, It.IsAny<bool>(), null))
                .Returns(Task.CompletedTask);
            _userManagerMock.Setup(manager => manager.GetRolesAsync(user))
                .ReturnsAsync(new List<string>()); // Ensure roles is not null

            // Act
            var result = await _loginController.Authenticate(loginDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var token = okResult.Value.GetType().GetProperty("Token").GetValue(okResult.Value, null) as string;
            Assert.NotNull(token);
        }


        [Fact]
        public async Task Authenticate_InvalidEmail_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "invalid@example.com", Password = "password" };
            _userManagerMock.Setup(manager => manager.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync((User)null);
            _problemDetailsFactoryMock.Setup(factory => factory.CreateProblemDetails(
                It.IsAny<HttpContext>(), StatusCodes.Status401Unauthorized, "Invalid email or password", null, null, null))
                .Returns(new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = "Invalid email or password" });

            // Act
            var result = await _loginController.Authenticate(loginDto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
            Assert.Equal(StatusCodes.Status401Unauthorized, problemDetails.Status);
            Assert.Equal("Invalid email or password", problemDetails.Title);
        }

        [Fact]
        public async Task Authenticate_UnconfirmedEmail_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "user@example.com", Password = "password" };
            var user = new User { Id = Guid.NewGuid(), Email = "user@example.com" };
            _userManagerMock.Setup(manager => manager.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(manager => manager.IsEmailConfirmedAsync(user))
                .ReturnsAsync(false);
            _problemDetailsFactoryMock.Setup(factory => factory.CreateProblemDetails(
                It.IsAny<HttpContext>(), StatusCodes.Status401Unauthorized, "Email not confirmed", null, null, null))
                .Returns(new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = "Email not confirmed" });

            // Act
            var result = await _loginController.Authenticate(loginDto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
            Assert.Equal(StatusCodes.Status401Unauthorized, problemDetails.Status);
            Assert.Equal("Email not confirmed", problemDetails.Title);
        }

        [Fact]
        public async Task Authenticate_InvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var loginDto = new LoginDto { Email = "user@example.com", Password = "invalidpassword" };
            var user = new User { Id = Guid.NewGuid(), Email = "user@example.com" };
            _userManagerMock.Setup(manager => manager.FindByEmailAsync(loginDto.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(manager => manager.IsEmailConfirmedAsync(user))
                .ReturnsAsync(true);
            _signInManagerMock.Setup(manager => manager.CheckPasswordSignInAsync(user, loginDto.Password, false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
            _problemDetailsFactoryMock.Setup(factory => factory.CreateProblemDetails(
                It.IsAny<HttpContext>(), StatusCodes.Status401Unauthorized, "Invalid email or password", null, null, null))
                .Returns(new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = "Invalid email or password" });

            // Act
            var result = await _loginController.Authenticate(loginDto);

            // Assert
            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(unauthorizedResult.Value);
            Assert.Equal(StatusCodes.Status401Unauthorized, problemDetails.Status);
            Assert.Equal("Invalid email or password", problemDetails.Title);
        }
    }
}










