using Famick.HomeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Famick.HomeManagement.Infrastructure.Configuration
{
    public class ShoppingLocationConfiguration : IEntityTypeConfiguration<ShoppingLocation>
    {
        public void Configure(EntityTypeBuilder<ShoppingLocation> builder)
        {
            builder.ToTable("shopping_locations");

            builder.HasKey(sl => sl.Id);

            builder.Property(sl => sl.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(sl => sl.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(sl => sl.Name)
                .HasColumnName("name")
                .HasColumnType("character varying(255)")
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(sl => sl.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            builder.Property(sl => sl.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(sl => sl.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes
            builder.HasIndex(sl => sl.TenantId)
                .HasDatabaseName("ix_shopping_locations_tenant_id");

            // Unique constraint on (TenantId, Name) for multi-tenancy
            builder.HasIndex(sl => new { sl.TenantId, sl.Name })
                .IsUnique()
                .HasDatabaseName("ux_shopping_locations_tenant_name");

            // Navigation properties
            builder.HasMany(sl => sl.Products)
                .WithOne(p => p.ShoppingLocation)
                .HasForeignKey(p => p.ShoppingLocationId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
