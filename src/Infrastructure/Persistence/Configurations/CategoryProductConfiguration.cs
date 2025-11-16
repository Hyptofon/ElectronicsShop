using Domain.Categories;
using Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CategoryProductConfiguration : IEntityTypeConfiguration<CategoryProduct>
{
    public void Configure(EntityTypeBuilder<CategoryProduct> builder)
    {
        builder.HasKey(cp => new { cp.CategoryId, cp.ProductId });

        builder.Property(x => x.CategoryId).HasConversion(x => x.Value, x => new CategoryId(x));
        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .HasConstraintName("fk_category_products_categories_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(x => x.ProductId).HasConversion(x => x.Value, x => new ProductId(x));
        builder.HasOne(x => x.Product)
            .WithMany(x => x.Categories)
            .HasForeignKey(x => x.ProductId)
            .HasConstraintName("fk_category_products_products_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}