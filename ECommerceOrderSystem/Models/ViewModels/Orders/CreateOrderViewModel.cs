using System.ComponentModel.DataAnnotations;

namespace ECommerceOrderSystem.Models.ViewModels.Orders;

public class CreateOrderViewModel
{
    [Required, StringLength(500)]
    [Display(Name = "Shipping address")]
    public string ShippingAddress { get; set; } = string.Empty;

    public List<CreateOrderItemViewModel> Items { get; set; } = [];
}

public class CreateOrderItemViewModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int AvailableStock { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }
}
