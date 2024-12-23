using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using ProductManagement.API.Controllers;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using Xunit;

namespace ProductManagement.API.Tests.Controllers
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductService> _productServiceMock;
        private readonly Mock<ProblemDetailsFactory> _problemDetailsFactoryMock;
        private readonly ProductsController _productsController;
        private readonly Guid _userId;

        public ProductsControllerTests()
        {
            _productServiceMock = new Mock<IProductService>();
            _problemDetailsFactoryMock = new Mock<ProblemDetailsFactory>();
            _productsController = new ProductsController(_productServiceMock.Object, _problemDetailsFactoryMock.Object);
            _userId = Guid.NewGuid();

            var httpContext = new DefaultHttpContext();
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) };
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            httpContext.User = claimsPrincipal;
            _productsController.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task GetProducts_ReturnsOkResult_WithListOfProducts()
        {
            // Arrange
            var products = new List<ProductReadDto> { new ProductReadDto { Id = Guid.NewGuid(), Name = "Product1" } };
            _productServiceMock.Setup(service => service.GetAllProductsAsync()).ReturnsAsync(products);

            // Act
            var result = await _productsController.GetProducts();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedProducts = Assert.IsType<List<ProductReadDto>>(okResult.Value);
            Assert.Single(returnedProducts);
        }

        [Fact]
        public async Task GetProduct_ReturnsOkResult_WithProduct()
        {
            // Arrange
            var productId = Guid.NewGuid();
            var product = new ProductReadDto { Id = productId, Name = "Product1" };
            _productServiceMock.Setup(service => service.GetProductByIdAsync(productId)).ReturnsAsync(product);

            // Act
            var result = await _productsController.GetProduct(productId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedProduct = Assert.IsType<ProductReadDto>(okResult.Value);
            Assert.Equal(productId, returnedProduct.Id);
        }

        [Fact]
        public async Task GetProduct_ReturnsNotFound_WhenProductDoesNotExist()
        {
            // Arrange
            var productId = Guid.NewGuid();
            _productServiceMock.Setup(service => service.GetProductByIdAsync(productId)).ReturnsAsync((ProductReadDto)null);
            _problemDetailsFactoryMock.Setup(factory => factory.CreateProblemDetails(
                It.IsAny<HttpContext>(), StatusCodes.Status404NotFound, "Product not found", null, null, null))
                .Returns(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Product not found" });

            // Act
            var result = await _productsController.GetProduct(productId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
            Assert.Equal("Product not found", problemDetails.Title);
        }

        [Fact]
        public async Task CreateProduct_ReturnsCreatedAtActionResult_WithCreatedProduct()
        {
            // Arrange
            var productCreateDto = new ProductCreateDto { Name = "Product1", Description = "Description1", Price = 100, Availability = true };
            var createdProduct = new ProductReadDto { Id = Guid.NewGuid(), Name = "Product1" };

            _productServiceMock.Setup(service => service.CreateProductAsync(productCreateDto, _userId)).ReturnsAsync(createdProduct);

            // Act
            var result = await _productsController.CreateProduct(productCreateDto);

            // Assert
            var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result);
            var returnedProduct = Assert.IsType<ProductReadDto>(createdAtActionResult.Value);
            Assert.Equal(createdProduct.Id, returnedProduct.Id);
        }

        [Fact]
        public async Task UpdateProduct_ReturnsNoContent_WhenProductIsUpdated()
        {
            // Arrange
            var productUpdateDto = new ProductUpdateDto { Id = Guid.NewGuid(), Name = "UpdatedProduct", Description = "UpdatedDescription", Price = 200, Availability = true };
            _productServiceMock.Setup(service => service.UpdateProductAsync(productUpdateDto, _userId)).ReturnsAsync(true);

            // Act
            var result = await _productsController.UpdateProduct(productUpdateDto.Id, productUpdateDto);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task UpdateProduct_ReturnsNotFound_WhenProductDoesNotExist()
        {
            // Arrange
            var productUpdateDto = new ProductUpdateDto { Id = Guid.NewGuid(), Name = "UpdatedProduct", Description = "UpdatedDescription", Price = 200, Availability = true };
            _productServiceMock.Setup(service => service.UpdateProductAsync(productUpdateDto, _userId)).ReturnsAsync(false);
            _problemDetailsFactoryMock.Setup(factory => factory.CreateProblemDetails(
                It.IsAny<HttpContext>(), StatusCodes.Status404NotFound, "Product not found", null, null, null))
                .Returns(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Product not found" });

            // Act
            var result = await _productsController.UpdateProduct(productUpdateDto.Id, productUpdateDto);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
            Assert.Equal("Product not found", problemDetails.Title);
        }

        [Fact]
        public async Task DeleteProduct_ReturnsNoContent_WhenProductIsDeleted()
        {
            // Arrange
            var productId = Guid.NewGuid();
            _productServiceMock.Setup(service => service.DeleteProductAsync(productId, _userId)).ReturnsAsync(true);

            // Act
            var result = await _productsController.DeleteProduct(productId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteProduct_ReturnsNotFound_WhenProductDoesNotExist()
        {
            // Arrange
            var productId = Guid.NewGuid();
            _productServiceMock.Setup(service => service.DeleteProductAsync(productId, _userId)).ReturnsAsync(false);
            _problemDetailsFactoryMock.Setup(factory => factory.CreateProblemDetails(
                It.IsAny<HttpContext>(), StatusCodes.Status404NotFound, "Product not found", null, null, null))
                .Returns(new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Product not found" });

            // Act
            var result = await _productsController.DeleteProduct(productId);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            var problemDetails = Assert.IsType<ProblemDetails>(notFoundResult.Value);
            Assert.Equal(StatusCodes.Status404NotFound, problemDetails.Status);
            Assert.Equal("Product not found", problemDetails.Title);
        }

        [Fact]
        public async Task SearchProducts_ReturnsOkResult_WithListOfProducts()
        {
            // Arrange
            var products = new List<ProductReadDto> { new ProductReadDto { Id = Guid.NewGuid(), Name = "Product1" } };
            _productServiceMock.Setup(service => service.SearchProductsAsync(It.IsAny<string>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(), It.IsAny<bool?>())).ReturnsAsync(products);

            // Act
            var result = await _productsController.SearchProducts(null, null, null, null);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedProducts = Assert.IsType<List<ProductReadDto>>(okResult.Value);
            Assert.Single(returnedProducts);
        }

        [Fact]
        public async Task SoftDeleteByUserId_ReturnsNoContent_WhenSoftDeleteIsSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _productServiceMock.Setup(service => service.SoftDeleteByUserIdAsync(userId)).Returns(Task.CompletedTask);

            // Act
            var result = await _productsController.SoftDeleteByUserId(userId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task RestoreByUserId_ReturnsNoContent_WhenRestoreIsSuccessful()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _productServiceMock.Setup(service => service.RestoreByUserIdAsync(userId)).Returns(Task.CompletedTask);

            // Act
            var result = await _productsController.RestoreByUserId(userId);

            // Assert
            Assert.IsType<NoContentResult>(result);
        }
    }
}

