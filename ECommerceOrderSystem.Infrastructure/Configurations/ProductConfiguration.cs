using ECommerceOrderSystem.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerceOrderSystem.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(product => product.Description)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(product => product.Price)
            .HasPrecision(18, 2);

        builder.Property(product => product.RowVersion)
            .IsRowVersion();

        builder.HasIndex(product => product.Name);

        builder.ToTable(table =>
        {
            table.HasCheckConstraint("CK_Products_Price", "[Price] > 0");
            table.HasCheckConstraint("CK_Products_Stock", "[Stock] >= 0");
        });
    }
}
