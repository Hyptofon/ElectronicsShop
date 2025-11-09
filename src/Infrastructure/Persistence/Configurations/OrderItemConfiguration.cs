using Domain.Orders;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new OrderItemId(x));

        builder.Property(x => x.OrderId).HasConversion(x => x.Value, x => new OrderId(x));
        builder.Property(x => x.ProductId).HasConversion(x => x.Value, x => new ProductId(x));

        builder.Property(x => x.Quantity).IsRequired();

        builder.Property(x => x.UnitPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .HasConstraintName("fk_order_items_orders_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}