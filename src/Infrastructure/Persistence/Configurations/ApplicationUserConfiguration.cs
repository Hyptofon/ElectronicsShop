using Domain.Users;
using Infrastructure.Persistence.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(x => x.FirstName)
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(x => x.LastName)
            .HasColumnType("varchar(100)")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .HasDefaultValueSql("timezone('utc', now())")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);

        builder.Property(x => x.IsBlocked)
            .HasDefaultValue(false)
            .IsRequired();
        
        builder.Property(x => x.RefreshToken) 
            .HasColumnType("varchar(500)")
            .IsRequired(false);
            
        builder.Property(x => x.RefreshTokenExpiryTime) 
            .HasConversion(new DateTimeUtcConverter())
            .IsRequired(false);
        
        builder.Property(x => x.RowVersion)
            .HasColumnType("bytea") 
            .IsRowVersion()
            .IsConcurrencyToken()
            .ValueGeneratedOnAddOrUpdate();
    }
}