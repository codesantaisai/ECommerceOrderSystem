namespace ECommerceOrderSystem.Models.ViewModels.Products;

public class DeleteProductViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
