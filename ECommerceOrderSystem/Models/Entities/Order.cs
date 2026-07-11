using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ECommerceOrderSystem.Common;

namespace ECommerceOrderSystem.Models.Entities;

public class Order
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(30)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    public string CustomerId { get; set; } = string.Empty;

    public ApplicationUser Customer { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal GrandTotal { get; set; }

    [Required, StringLength(500)]
    public string ShippingAddress { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedDate { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
}
