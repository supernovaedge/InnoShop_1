using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProductManagement.Domain.Entities;

namespace ProductManagement.Application.Interfaces
{
    public interface IProductRepository
    {
        Task<Product> GetByIdAsync(Guid id);
        Task<IEnumerable<Product>> GetAllAsync();
        Task<IEnumerable<Product>> SearchAsync(string? name, decimal? minPrice, decimal? maxPrice, bool? availability);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task SoftDeleteByUserIdAsync(Guid id);
        Task RestoreByUserIdAsync(Guid id);
    }
}
