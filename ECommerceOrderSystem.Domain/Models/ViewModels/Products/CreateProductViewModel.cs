using System.ComponentModel.DataAnnotations;

namespace ECommerceOrderSystem.Models.ViewModels.Products;

public class CreateProductViewModel
{
    [Required, StringLength(120)]
    [Display(Name = "Product name")]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [Range(typeof(decimal), "0.01", "9999999999999999")]
    [DataType(DataType.Currency)]
    public decimal Price { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    [Display(Name = "Stock quantity")]
    public int Stock { get; set; }
}
