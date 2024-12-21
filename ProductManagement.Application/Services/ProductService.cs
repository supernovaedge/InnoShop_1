using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using ProductManagement.Application.DTOs;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;

        public ProductService(IProductRepository productRepository, IMapper mapper)
        {
            _productRepository = productRepository;
            _mapper = mapper;
        }

        public async Task<ProductReadDto> CreateProductAsync(ProductCreateDto productCreateDto, Guid userId)
        {
            var product = _mapper.Map<Product>(productCreateDto);
            product.Id = Guid.NewGuid();
            product.UserId = userId;
            product.CreatedDate = DateTime.UtcNow;
            await _productRepository.AddAsync(product);
            return _mapper.Map<ProductReadDto>(product);
        }

        public async Task<bool> UpdateProductAsync(ProductUpdateDto productUpdateDto, Guid userId)
        {
            var product = await _productRepository.GetByIdAsync(productUpdateDto.Id);
            if (product == null || product.IsDeleted || product.UserId != userId)
                return false;

            _mapper.Map(productUpdateDto, product);
            await _productRepository.UpdateAsync(product);
            return true;
        }

        public async Task<bool> DeleteProductAsync(Guid id, Guid userId)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null || product.IsDeleted || product.UserId != userId)
                return false;

            product.IsDeleted = true;
            await _productRepository.UpdateAsync(product);
            return true;
        }

        public async Task<IEnumerable<ProductReadDto>> GetAllProductsAsync()
        {
            var products = await _productRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductReadDto>>(products);
        }

        public async Task<ProductReadDto> GetProductByIdAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            return _mapper.Map<ProductReadDto>(product);
        }

        public async Task<IEnumerable<ProductReadDto>> SearchProductsAsync(string? name, decimal? minPrice, decimal? maxPrice, bool? availability)
        {
            var products = await _productRepository.SearchAsync(name, minPrice, maxPrice, availability);
            return _mapper.Map<IEnumerable<ProductReadDto>>(products);
        }
    }
}
