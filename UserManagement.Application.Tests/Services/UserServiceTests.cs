using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using UserManagement.Application.DTOs;
using UserManagement.Application.Interfaces;
using UserManagement.Application.Mapping;
using UserManagement.Application.Services;
using UserManagement.Core.Interfaces;
using UserManagement.Domain.Entities;
using Xunit;

namespace UserManagement.Application.Tests.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly IMapper _mapper;
        private readonly Mock<ILogger<UserService>> _loggerMock;
        private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private readonly HttpClient _httpClient;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _loggerMock = new Mock<ILogger<UserService>>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
            {
                BaseAddress = new Uri("http://localhost")
            };
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<UserMappingProfile>();
            });
            _mapper = configuration.CreateMapper();

            _userService = new UserService(_httpClient, _userRepositoryMock.Object, _mapper, _loggerMock.Object, _httpContextAccessorMock.Object);
        }

        [Fact]
        public async Task GetUserByIdAsync_ValidId_ReturnsUserDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "Test User" };

            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(user);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
            Assert.Equal(user.Name, result.Name);
        }

        [Fact]
        public async Task GetAllUsersAsync_ReturnsUserDtoList()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Name = "User 1" },
                new User { Id = Guid.NewGuid(), Name = "User 2" }
            };

            _userRepositoryMock.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(users);

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, ((List<UserDto>)result).Count);
        }

        [Fact]
        public async Task AddUserAsync_ValidInput_AddsUser()
        {
            // Arrange
            var createUserDto = new CreateUserDto
            {
                Name = "New User",
                Email = "newuser@example.com",
                Password = "password",
                Role = "User"
            };

            // Act
            await _userService.AddUserAsync(createUserDto);

            // Assert
            _userRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<User>()), Times.Once);
        }

        [Fact]
        public async Task UpdateUserAsync_UserExists_UpdatesUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var updateUserDto = new UpdateUserDto
            {
                Id = userId,
                Name = "Updated User",
                Email = "updateduser@example.com",
                Role = "Admin",
                IsActive = false
            };

            var existingUser = new User
            {
                Id = userId,
                Name = "Existing User",
                IsActive = true
            };

            _userRepositoryMock.Setup(repo => repo.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);

            _httpContextAccessorMock.Setup(accessor => accessor.HttpContext.Request.Headers["Authorization"])
                .Returns("Bearer test_token");

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString().Contains($"/api/products/softdelete/{userId}")),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            // Act
            await _userService.UpdateUserAsync(updateUserDto);

            // Assert
            _userRepositoryMock.Verify(repo => repo.UpdateAsync(existingUser), Times.Once);
            _httpMessageHandlerMock.Protected().Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.ToString().Contains($"/api/products/softdelete/{userId}")),
                ItExpr.IsAny<CancellationToken>()
            );
        }

        [Fact]
        public async Task DeleteUserAsync_ValidId_DeletesUser()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            await _userService.DeleteUserAsync(userId);

            // Assert
            _userRepositoryMock.Verify(repo => repo.DeleteAsync(userId), Times.Once);
        }
    }
}

