using ECommerceOrderSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceOrderSystem.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(item => item.Id);

        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(item => item.LineTotal)
            .HasPrecision(18, 2);

        builder.HasIndex(item => new { item.OrderId, item.ProductId })
            .IsUnique();

        builder.HasOne(item => item.Order)
            .WithMany(order => order.Items)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Product)
            .WithMany(product => product.OrderItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_OrderItems_Quantity", "[Quantity] > 0");
            table.HasCheckConstraint("CK_OrderItems_UnitPrice", "[UnitPrice] > 0");
            table.HasCheckConstraint("CK_OrderItems_LineTotal", "[LineTotal] > 0");
        });
    }
}
