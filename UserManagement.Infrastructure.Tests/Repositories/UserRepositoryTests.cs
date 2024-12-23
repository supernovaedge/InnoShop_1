using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using UserManagement.Domain.Entities;
using UserManagement.Infrastructure.Data;
using UserManagement.Infrastructure.Repositories;
using Xunit;

namespace UserManagement.Infrastructure.Tests.Repositories
{
    public class UserRepositoryTests
    {
        private readonly UserManagementDbContext _context;
        private readonly UserRepository _userRepository;

        public UserRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<UserManagementDbContext>()
                .UseInMemoryDatabase(databaseName: "UserManagementTestDb")
                .Options;
            _context = new UserManagementDbContext(options);
            _userRepository = new UserRepository(_context);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsUser_WhenUserExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "Test User" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userRepository.GetByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(userId, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var result = await _userRepository.GetByIdAsync(userId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            // Arrange
            var users = new List<User>
            {
                new User { Id = Guid.NewGuid(), Name = "User1" },
                new User { Id = Guid.NewGuid(), Name = "User2" }
            };
            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _userRepository.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task AddAsync_AddsUser()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Name = "New User" };

            // Act
            await _userRepository.AddAsync(user);

            // Assert
            var addedUser = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(addedUser);
            Assert.Equal(user.Name, addedUser.Name);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesUser()
        {
            // Arrange
            var user = new User { Id = Guid.NewGuid(), Name = "Original User" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            user.Name = "Updated User";
            await _userRepository.UpdateAsync(user);

            // Assert
            var updatedUser = await _context.Users.FindAsync(user.Id);
            Assert.NotNull(updatedUser);
            Assert.Equal("Updated User", updatedUser.Name);
        }

        [Fact]
        public async Task DeleteAsync_DeletesUser_WhenUserExists()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var user = new User { Id = userId, Name = "User to Delete" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            await _userRepository.DeleteAsync(userId);

            // Assert
            var deletedUser = await _context.Users.FindAsync(userId);
            Assert.Null(deletedUser);
        }

        [Fact]
        public async Task DeleteAsync_DoesNothing_WhenUserDoesNotExist()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            await _userRepository.DeleteAsync(userId);

            // Assert
            var user = await _context.Users.FindAsync(userId);
            Assert.Null(user);
        }
    }
}


