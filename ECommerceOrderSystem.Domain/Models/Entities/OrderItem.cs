using ECommerceOrderSystem.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceOrderSystem.Models.Entities;

public class OrderItem : BaseModel
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal LineTotal { get; set; }
}
