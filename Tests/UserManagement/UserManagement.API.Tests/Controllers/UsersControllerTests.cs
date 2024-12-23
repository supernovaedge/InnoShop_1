using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using UserManagement.API.Controllers;
using UserManagement.Application.DTOs;
using UserManagement.Application.Interfaces;
using UserManagement.Domain.Entities;
using Xunit;

namespace UserManagement.API.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
        private readonly Mock<ProblemDetailsFactory> _problemDetailsFactoryMock;
        private readonly UsersController _usersController;

        public UsersControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _userManagerMock = new Mock<UserManager<User>>(
                Mock.Of<IUserStore<User>>(), null, null, null, null, null, null, null, null);
            _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
                Mock.Of<IRoleStore<IdentityRole<Guid>>>(), null, null, null, null);
            _problemDetailsFactoryMock = new Mock<ProblemDetailsFactory>();

            _usersController = new UsersController(
                _userServiceMock.Object,
                _userManagerMock.Object,
                _roleManagerMock.Object,
                null, // Pass null for IEmailSender
                _problemDetailsFactoryMock.Object);
        }

        [Fact]
        public async Task GetUserById_ValidId_ReturnsOkResult()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var userDto = new UserDto { Id = userId, Name = "Test User" };
            _userServiceMock.Setup(service => service.GetUserByIdAsync(userId))
                .ReturnsAsync(userDto);

            // Act
            var result = await _usersController.GetUserById(userId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUser = Assert.IsType<UserDto>(okResult.Value);
            Assert.Equal(userId, returnedUser.Id);
        }

        [Fact]
        public async Task GetUserById_InvalidId_ReturnsNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userServiceMock.Setup(service => service.GetUserByIdAsync(userId))
                .ReturnsAsync((UserDto)null);
            _problemDetailsFactoryMock.Setup(factory => factory.CreateProblemDetails(
                It.IsAny<HttpContext>(), StatusCodes.Status404NotFound, "User not found", null, null, null))
                .Returns(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "User not found" });

            // Act
            var result = await _usersController.GetUserById(userId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
            Assert.Equal("User not found", problemDetails.Title);
        }

        [Fact]
        public async Task GetAllUsers_ReturnsOkResult()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto { Id = Guid.NewGuid(), Name = "User 1" },
                new UserDto { Id = Guid.NewGuid(), Name = "User 2" }
            };
            _userServiceMock.Setup(service => service.GetAllUsersAsync())
                .ReturnsAsync(users);

            // Act
            var result = await _usersController.GetAllUsers();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedUsers = Assert.IsType<List<UserDto>>(okResult.Value);
            Assert.Equal(2, returnedUsers.Count);
        }

        [Fact]
        public async Task UpdateUser_ValidInput_ReturnsNoContent()
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

            // Act
            var result = await _usersController.UpdateUser(userId, updateUserDto);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteUser_ValidId_ReturnsNoContent()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _usersController.DeleteUser(userId);

            // Assert
            var noContentResult = Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task ConfirmEmail_ValidInput_ReturnsOk()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var token = "valid-token";
            var user = new User { Id = userId, Email = "user@example.com" };
            _userManagerMock.Setup(manager => manager.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(user);
            _userManagerMock.Setup(manager => manager.ConfirmEmailAsync(user, token))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _usersController.ConfirmEmail(userId, token);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Email confirmed successfully!", okResult.Value);
        }

        [Fact]
        public async Task ResetPassword_ValidInput_ReturnsOk()
        {
            // Arrange
            var resetPasswordDto = new ResetPasswordDto
            {
                Email = "user@example.com",
                Token = "reset-token",
                NewPassword = "new-password"
            };
            var user = new User { Id = Guid.NewGuid(), Email = "user@example.com" };
            _userManagerMock.Setup(manager => manager.FindByEmailAsync(resetPasswordDto.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(manager => manager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _usersController.ResetPassword(resetPasswordDto);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("Password has been reset successfully.", okResult.Value);
        }
    }
}
