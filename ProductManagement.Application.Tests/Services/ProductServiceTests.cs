using AutoMapper;
using Moq;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using ProductManagement.Application.Services;
using ProductManagement.Domain.Entities;
using ProductManagement.Application.Mapping;
using Xunit;

namespace ProductManagement.Application.Tests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _productRepositoryMock;
        private readonly IMapper _mapper;
        private readonly ProductService _productService;

        public ProductServiceTests()
        {
            _productRepositoryMock = new Mock<IProductRepository>();

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ProductMappingProfile>();
            });
            _mapper = configuration.CreateMapper();

            _productService = new ProductService(_productRepositoryMock.Object, _mapper);
        }

        [Fact]
        public async Task CreateProductAsync_ValidInput_ReturnsProductReadDto()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productCreateDto = new ProductCreateDto
            {
                Name = "Test Product",
                Description = "Test Description",
                Price = 10.99m,
                Availability = true
            };

            // Act
            var result = await _productService.CreateProductAsync(productCreateDto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(productCreateDto.Name, result.Name);
            Assert.Equal(productCreateDto.Description, result.Description);
            Assert.Equal(productCreateDto.Price, result.Price);
            Assert.Equal(productCreateDto.Availability, result.Availability);
            Assert.Equal(userId, result.UserId);

            // Verify that AddAsync was called once
            _productRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<Product>()), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_ProductExists_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var productUpdateDto = new ProductUpdateDto
            {
                Id = productId,
                Name = "Updated Product",
                Description = "Updated Description",
                Price = 15.99m,
                Availability = false
            };

            var existingProduct = new Product
            {
                Id = productId,
                UserId = userId,
                IsDeleted = false
            };

            _productRepositoryMock.Setup(repo => repo.GetByIdAsync(productId))
                .ReturnsAsync(existingProduct);

            // Act
            var result = await _productService.UpdateProductAsync(productUpdateDto, userId);

            // Assert
            Assert.True(result);

            // Verify that UpdateAsync was called once
            _productRepositoryMock.Verify(repo => repo.UpdateAsync(existingProduct), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_ProductDoesNotExist_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productUpdateDto = new ProductUpdateDto
            {
                Id = Guid.NewGuid(),
                Name = "Updated Product",
                Description = "Updated Description",
                Price = 15.99m,
                Availability = false
            };

            _productRepositoryMock.Setup(repo => repo.GetByIdAsync(productUpdateDto.Id))
                .ReturnsAsync((Product)null);

            // Act
            var result = await _productService.UpdateProductAsync(productUpdateDto, userId);

            // Assert
            Assert.False(result);

            // Verify that UpdateAsync was not called
            _productRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task DeleteProductAsync_ProductExists_ReturnsTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var existingProduct = new Product
            {
                Id = productId,
                UserId = userId,
                IsDeleted = false
            };

            _productRepositoryMock.Setup(repo => repo.GetByIdAsync(productId))
                .ReturnsAsync(existingProduct);

            // Act
            var result = await _productService.DeleteProductAsync(productId, userId);

            // Assert
            Assert.True(result);
            Assert.True(existingProduct.IsDeleted);

            // Verify that UpdateAsync was called once
            _productRepositoryMock.Verify(repo => repo.UpdateAsync(existingProduct), Times.Once);
        }

        [Fact]
        public async Task DeleteProductAsync_ProductIsDeletedOrNotOwned_ReturnsFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var anotherUserId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            var existingProduct = new Product
            {
                Id = productId,
                UserId = anotherUserId,
                IsDeleted = false
            };

            _productRepositoryMock.Setup(repo => repo.GetByIdAsync(productId))
                .ReturnsAsync(existingProduct);

            // Act
            var result = await _productService.DeleteProductAsync(productId, userId);

            // Assert
            Assert.False(result);

            // Verify that UpdateAsync was not called
            _productRepositoryMock.Verify(repo => repo.UpdateAsync(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task GetAllProductsAsync_ReturnsProductReadDtoList()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Product 1" },
                new Product { Id = Guid.NewGuid(), Name = "Product 2" }
            };

            _productRepositoryMock.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(products);

            // Act
            var result = await _productService.GetAllProductsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, ((List<ProductReadDto>)result).Count);
        }

        [Fact]
        public async Task GetProductByIdAsync_ProductExists_ReturnsProductReadDto()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var product = new Product { Id = productId, Name = "Product" };

            _productRepositoryMock.Setup(repo => repo.GetByIdAsync(productId))
                .ReturnsAsync(product);

            // Act
            var result = await _productService.GetProductByIdAsync(productId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(productId, result.Id);
        }

        [Fact]
        public async Task GetProductByIdAsync_ProductDoesNotExist_ReturnsNull()
        {
            // Arrange
            var productId = Guid.NewGuid();

            _productRepositoryMock.Setup(repo => repo.GetByIdAsync(productId))
                .ReturnsAsync((Product)null);

            // Act
            var result = await _productService.GetProductByIdAsync(productId);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task SearchProductsAsync_ReturnsFilteredProducts()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { Id = Guid.NewGuid(), Name = "Product A", Price = 10m, Availability = true },
                new Product { Id = Guid.NewGuid(), Name = "Product B", Price = 20m, Availability = false }
            };

            _productRepositoryMock.Setup(repo => repo.SearchAsync("Product", null, null, null))
                .ReturnsAsync(products);

            // Act
            var result = await _productService.SearchProductsAsync("Product", null, null, null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, ((List<ProductReadDto>)result).Count);
        }
    }
}
