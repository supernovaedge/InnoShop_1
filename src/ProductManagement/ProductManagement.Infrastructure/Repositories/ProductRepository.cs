using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductManagement.Application.Interfaces;
using ProductManagement.Domain.Entities;
using ProductManagement.Infrastructure.Data;

namespace ProductManagement.Infrastructure.Repositories
{
    public class ProductRepository : IProductRepository
    {
        private readonly ProductManagementDbContext _context;
        private readonly ILogger<ProductRepository> _logger;

        public ProductRepository(ProductManagementDbContext context, ILogger<ProductRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task AddAsync(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _context.Products
                .Where(p => !p.IsDeleted)
                .ToListAsync();
        }

        public async Task<Product> GetByIdAsync(Guid id)
        {
            return await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        }

        public async Task<IEnumerable<Product>> SearchAsync(string? name, decimal? minPrice, decimal? maxPrice, bool? availability)
        {
            var query = _context.Products.AsQueryable();

            query = query.Where(p => !p.IsDeleted);

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(p => p.Name.Contains(name));
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            if (availability.HasValue)
            {
                query = query.Where(p => p.Availability == availability.Value);
            }

            return await query.ToListAsync();
        }

        public async Task SoftDeleteByUserIdAsync(Guid userId)
        {
            var products = await _context.Products
                .Where(p => p.UserId == userId)
                .ToListAsync();

            foreach (var product in products)
            {
                product.IsDeleted = true;
            }

            _context.Products.UpdateRange(products);
            await _context.SaveChangesAsync();
        }

        public async Task RestoreByUserIdAsync(Guid userId)
        {
            var products = await _context.Products
                .IgnoreQueryFilters()
                .Where(p => p.UserId == userId && p.IsDeleted)
                .ToListAsync();

            if (products == null || !products.Any())
            {
                return;
            }

            foreach (var product in products)
            {
                product.IsDeleted = false;
            }

            _context.Products.UpdateRange(products);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Product product)
        {
            _context.Products.Update(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
            }
        }
    }
}
