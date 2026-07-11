using ECommerceOrderSystem.Models.Common;

namespace ECommerceOrderSystem.Models.ViewModels.Orders;

public class OrderListItemViewModel
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public int ItemCount { get; set; }
    public decimal GrandTotal { get; set; }
    public OrderStatus Status { get; set; }
}
