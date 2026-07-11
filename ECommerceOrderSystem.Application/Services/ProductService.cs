using ECommerceOrderSystem.Application.Services.Interface;
using ECommerceOrderSystem.Models.Entities;
using ECommerceOrderSystem.Models.ViewModels.Products;
using Microsoft.EntityFrameworkCore;
using ECommerceOrderSystem.Data;

namespace ECommerceOrderSystem.Services;

public sealed class ProductService(ApplicationDbContext dbContext) : IProductService
{
    public async Task<IReadOnlyList<Product>> GetProductsAsync(string search = "")
    {
        var query = dbContext.Products.AsNoTracking();
        if(!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(product => product.Name.Contains(search));
        }
        return await query.OrderByDescending(product => product.CreatedDate).ToListAsync();
    }

    public Task<Product?> GetByIdAsync(Guid id) =>
        dbContext.Products.AsNoTracking().FirstOrDefaultAsync(product => product.Id == id);

    public async Task<Product> CreateAsync(CreateProductViewModel model)
    {
        var product = new Product { Name = model.Name.Trim(), Description = model.Description.Trim(), Price = model.Price, Stock = model.Stock, CreatedDate = DateTime.UtcNow };
        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();
        return product;
    }

    public async Task<EditProductViewModel?> GetForEditAsync(Guid id)
    {
        var product = await GetByIdAsync(id);
        return product is null ? null : new EditProductViewModel { Id = product.Id, Name = product.Name, Description = product.Description, Price = product.Price, Stock = product.Stock, RowVersion = product.RowVersion };
    }

    public async Task<ProductOperationResult> UpdateAsync(Guid id, EditProductViewModel model)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(item => item.Id == id);
        if(product is null) return ProductOperationResult.Failure("This product no longer exists.", true);
        dbContext.Entry(product).Property(item => item.RowVersion).OriginalValue = model.RowVersion;
        product.Name = model.Name.Trim(); product.Description = model.Description.Trim(); product.Price = model.Price; product.Stock = model.Stock;
        try
        {
            await dbContext.SaveChangesAsync();
            return ProductOperationResult.Success($"{product.Name} was updated successfully.");
        }
        catch(DbUpdateConcurrencyException exception)
        {
            var values = await exception.Entries.Single().GetDatabaseValuesAsync();
            return values is null
                ? ProductOperationResult.Failure("This product was deleted by another administrator.", true)
                : ProductOperationResult.Failure("This product was changed by another administrator. Review your values and save again.", rowVersion: ((Product)values.ToObject()).RowVersion);
        }
    }

    public async Task<DeleteProductViewModel?> GetForDeleteAsync(Guid id)
    {
        var product = await GetByIdAsync(id);
        return product is null ? null : new DeleteProductViewModel { Id = product.Id, Name = product.Name, Description = product.Description, Price = product.Price, Stock = product.Stock, RowVersion = product.RowVersion };
    }

    public async Task<ProductOperationResult> DeleteAsync(Guid id, byte[] rowVersion)
    {
        var product = await dbContext.Products.FirstOrDefaultAsync(item => item.Id == id);
        if(product is null) return ProductOperationResult.Failure("The product no longer exists.", true);
        if(await dbContext.OrderItems.AnyAsync(item => item.ProductId == id)) return ProductOperationResult.Failure("This product belongs to one or more orders and cannot be deleted because order history must be preserved.");
        dbContext.Entry(product).Property(item => item.RowVersion).OriginalValue = rowVersion;
        dbContext.Products.Remove(product);
        try { await dbContext.SaveChangesAsync(); return ProductOperationResult.Success($"{product.Name} was deleted successfully."); }
        catch(DbUpdateConcurrencyException) { return ProductOperationResult.Failure("The product was changed or deleted by another administrator. Nothing was deleted."); }
        catch(DbUpdateException) { return ProductOperationResult.Failure("The product could not be deleted because it is in use."); }
    }
}


