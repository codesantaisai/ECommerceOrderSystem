using ECommerceOrderSystem.Models.Common;

namespace ECommerceOrderSystem.Models.ViewModels.Orders;

public class OrderDetailsViewModel
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public decimal GrandTotal { get; set; }
    public OrderStatus Status { get; set; }
    public string ShippingAddress { get; set; } = string.Empty;
    public IReadOnlyList<OrderDetailsItemViewModel> Items { get; set; } = [];
}

public class OrderDetailsItemViewModel
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
