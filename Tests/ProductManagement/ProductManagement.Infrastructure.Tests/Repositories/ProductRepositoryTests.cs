using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using ProductManagement.Domain.Entities;
using ProductManagement.Infrastructure.Data;
using ProductManagement.Infrastructure.Repositories;
using Xunit;

namespace ProductManagement.Infrastructure.Tests.Repositories
{
    public class ProductRepositoryTests : IDisposable
    {
        private readonly ProductManagementDbContext _context;
        private readonly ProductRepository _productRepository;
        private readonly Mock<ILogger<ProductRepository>> _loggerMock;

        public ProductRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ProductManagementDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ProductManagementDbContext(options);
            _loggerMock = new Mock<ILogger<ProductRepository>>();
            _productRepository = new ProductRepository(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsProduct_WhenProductExists()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var product = new Product { Id = productId, Name = "Test Product", IsDeleted = false };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            var result = await _productRepository.GetByIdAsync(productId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(productId, result.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenProductDoesNotExist()
        {
            // Arrange
            var productId = Guid.NewGuid();

            // Act
            var result = await _productRepository.GetByIdAsync(productId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllProducts()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Product1", IsDeleted = false },
                new Product { Id = Guid.NewGuid(), Name = "Product2", IsDeleted = false }
            };
            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            // Act
            var result = await _productRepository.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task AddAsync_AddsProduct()
        {
            // Arrange
            var product = new Product { Id = Guid.NewGuid(), Name = "New Product" };

            // Act
            await _productRepository.AddAsync(product);

            // Assert
            var addedProduct = await _context.Products.FindAsync(product.Id);
            Assert.NotNull(addedProduct);
            Assert.Equal(product.Name, addedProduct.Name);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesProduct()
        {
            // Arrange
            var product = new Product { Id = Guid.NewGuid(), Name = "Original Product" };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            product.Name = "Updated Product";
            await _productRepository.UpdateAsync(product);

            // Assert
            var updatedProduct = await _context.Products.FindAsync(product.Id);
            Assert.NotNull(updatedProduct);
            Assert.Equal("Updated Product", updatedProduct.Name);
        }

        [Fact]
        public async Task DeleteAsync_DeletesProduct_WhenProductExists()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var product = new Product { Id = productId, Name = "Product to Delete" };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Act
            await _productRepository.DeleteAsync(productId);

            // Assert
            var deletedProduct = await _context.Products.FindAsync(productId);
            Assert.Null(deletedProduct);
        }

        [Fact]
        public async Task SearchAsync_ReturnsMatchingProducts()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Product1", Price = 100, Availability = true, IsDeleted = false },
                new Product { Id = Guid.NewGuid(), Name = "Product2", Price = 200, Availability = false, IsDeleted = false }
            };
            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            // Act
            var result = await _productRepository.SearchAsync("Product1", 50, 150, true);

            // Assert
            Assert.Single(result);
            Assert.Equal("Product1", result.First().Name);
        }

        [Fact]
        public async Task SoftDeleteByUserIdAsync_SoftDeletesProducts()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var products = new List<Product>
    {
        new Product { Id = Guid.NewGuid(), Name = "Product1", UserId = userId, IsDeleted = false },
        new Product { Id = Guid.NewGuid(), Name = "Product2", UserId = userId, IsDeleted = false }
    };
            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            // Act
            await _productRepository.SoftDeleteByUserIdAsync(userId);

            // Assert
            var softDeletedProducts = await _context.Products.IgnoreQueryFilters().Where(p => p.UserId == userId && p.IsDeleted).ToListAsync();
            Assert.Equal(2, softDeletedProducts.Count);
        }


        [Fact]
        public async Task RestoreByUserIdAsync_RestoresSoftDeletedProducts()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Product1", UserId = userId, IsDeleted = true },
                new Product { Id = Guid.NewGuid(), Name = "Product2", UserId = userId, IsDeleted = true }
            };
            _context.Products.AddRange(products);
            await _context.SaveChangesAsync();

            // Act
            await _productRepository.RestoreByUserIdAsync(userId);

            // Assert
            var restoredProducts = await _context.Products.IgnoreQueryFilters().Where(p => p.UserId == userId && !p.IsDeleted).ToListAsync();
            Assert.Equal(2, restoredProducts.Count);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}



