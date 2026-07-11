using System.ComponentModel.DataAnnotations;

namespace ECommerceOrderSystem.Models.ViewModels.Products;

public class EditProductViewModel : CreateProductViewModel
{
    [Required]
    public Guid Id { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
