using Famick.HomeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Famick.HomeManagement.Infrastructure.Configuration
{
    public class ProductCommonNameConfiguration : IEntityTypeConfiguration<ProductCommonName>
    {
        public void Configure(EntityTypeBuilder<ProductCommonName> builder)
        {
            builder.ToTable("product_common_names");

            builder.HasKey(pcn => pcn.Id);

            builder.Property(pcn => pcn.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(pcn => pcn.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(pcn => pcn.Name)
                .HasColumnName("name")
                .HasColumnType("character varying(300)")
                .IsRequired()
                .HasMaxLength(300);

            builder.Property(pcn => pcn.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            builder.Property(pcn => pcn.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(pcn => pcn.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes
            builder.HasIndex(pcn => pcn.TenantId)
                .HasDatabaseName("ix_product_common_names_tenant_id");

            // Unique constraint on (TenantId, Name) for multi-tenancy
            builder.HasIndex(pcn => new { pcn.TenantId, pcn.Name })
                .IsUnique()
                .HasDatabaseName("ux_product_common_names_tenant_name");

            // Navigation properties
            builder.HasMany(pcn => pcn.Products)
                .WithOne(p => p.ProductCommonName)
                .HasForeignKey(p => p.ProductCommonNameId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
