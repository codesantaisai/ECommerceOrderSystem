using ECommerceOrderSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceOrderSystem.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(order => order.Id);

        builder.Property(order => order.OrderNumber)
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(order => order.CustomerId)
            .IsRequired();

        builder.Property(order => order.GrandTotal)
            .HasPrecision(18, 2);

        builder.Property(order => order.ShippingAddress)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(order => order.OrderNumber)
            .IsUnique();

        builder.HasIndex(order => new { order.CustomerId, order.CreatedDate });
        builder.HasIndex(order => order.Status);

        builder.HasOne(order => order.Customer)
            .WithMany(user => user.Orders)
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(table =>
            table.HasCheckConstraint("CK_Orders_GrandTotal", "[GrandTotal] >= 0"));
    }
}
