using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasConversion(x => x.Value, x => new ProductImageId(x));

        builder.Property(x => x.OriginalName)
            .IsRequired()
            .HasColumnType("varchar(255)");

        builder.Property(x => x.IsPrimary)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.ProductId).HasConversion(x => x.Value, x => new ProductId(x));
        builder.HasOne(x => x.Product)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductId)
            .HasConstraintName("fk_product_images_products_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}