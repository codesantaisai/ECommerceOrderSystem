using ECommerceOrderSystem.Models.Entities;
using ECommerceOrderSystem.Models.ViewModels.Products;

namespace ECommerceOrderSystem.Application.Services.Interface;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetProductsAsync(string search = "");
    Task<Product> GetByIdAsync(Guid id);
    Task<Product> CreateAsync(CreateProductViewModel model);
    Task<EditProductViewModel> GetForEditAsync(Guid id);
    Task<ProductOperationResult> UpdateAsync(Guid id, EditProductViewModel model);
    Task<DeleteProductViewModel> GetForDeleteAsync(Guid id);
    Task<ProductOperationResult> DeleteAsync(Guid id, byte[] rowVersion);
}

public sealed record ProductOperationResult(bool Succeeded, string Message, bool NotFound = false, byte[] RowVersion = null)
{
    public static ProductOperationResult Success(string message) => new(true, message);
    public static ProductOperationResult Failure(string message, bool notFound = false, byte[] rowVersion = null) => new(false, message, notFound, rowVersion);
}
