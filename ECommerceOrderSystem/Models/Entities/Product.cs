using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerceOrderSystem.Models.Entities;

public class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    [Range(typeof(decimal), "0.01", "9999999999999999")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue)]
    public int Stock { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    public ICollection<OrderItem> OrderItems { get; set; } = [];
}
