using ProductManagement.Application.DTOs;

public interface IProductService
{
    Task<ProductReadDto> GetProductByIdAsync(Guid id);
    Task<IEnumerable<ProductReadDto>> GetAllProductsAsync();
    Task<IEnumerable<ProductReadDto>> SearchProductsAsync(string? name, decimal? minPrice, decimal? maxPrice, bool? availability);
    Task<ProductReadDto> CreateProductAsync(ProductCreateDto productCreateDto, Guid userId);
    Task<bool> UpdateProductAsync(ProductUpdateDto productUpdateDto, Guid userId);
    Task<bool> DeleteProductAsync(Guid id, Guid userId);
    Task SoftDeleteByUserIdAsync(Guid userId);
    Task RestoreByUserIdAsync(Guid userId);
}
