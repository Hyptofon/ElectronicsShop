using Domain.Cart;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new CartItemId(x));

        builder.Property(x => x.CartId).HasConversion(x => x.Value, x => new CartId(x));
        builder.Property(x => x.ProductId).HasConversion(x => x.Value, x => new ProductId(x));

        builder.Property(x => x.Quantity).IsRequired();

        builder.HasOne(x => x.Cart)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.CartId)
            .HasConstraintName("fk_cart_items_carts_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();
    }
}