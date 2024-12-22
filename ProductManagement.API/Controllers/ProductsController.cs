using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace ProductManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ProblemDetailsFactory _problemDetailsFactory;

        public ProductsController(IProductService productService, ProblemDetailsFactory problemDetailsFactory)
        {
            _productService = productService;
            _problemDetailsFactory = problemDetailsFactory;
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(Guid id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status404NotFound, "Product not found"));
            return Ok(product);
        }

        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] ProductCreateDto productCreateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(_problemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));

            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
                return Unauthorized(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized"));

            var createdProduct = await _productService.CreateProductAsync(productCreateDto, userId);
            return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] ProductUpdateDto productUpdateDto)
        {
            if (id != productUpdateDto.Id)
                return BadRequest(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status400BadRequest, "Product ID mismatch"));

            if (!ModelState.IsValid)
                return BadRequest(_problemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));

            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
                return Unauthorized(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized"));

            var updated = await _productService.UpdateProductAsync(productUpdateDto, userId);
            if (!updated)
                return NotFound(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status404NotFound, "Product not found"));
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(Guid id)
        {
            var userId = GetUserIdFromClaims();
            if (userId == Guid.Empty)
                return Unauthorized(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized"));

            var deleted = await _productService.DeleteProductAsync(id, userId);
            if (!deleted)
                return NotFound(_problemDetailsFactory.CreateProblemDetails(HttpContext, StatusCodes.Status404NotFound, "Product not found"));
            return NoContent();
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchProducts([FromQuery] string? name, [FromQuery] decimal? minPrice, [FromQuery] decimal? maxPrice, [FromQuery] bool? availability)
        {
            var products = await _productService.SearchProductsAsync(name, minPrice, maxPrice, availability);
            return Ok(products);
        }

        private Guid GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}

