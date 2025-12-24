using Famick.HomeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Famick.HomeManagement.Infrastructure.Configuration
{
    public class ShoppingListItemConfiguration : IEntityTypeConfiguration<ShoppingListItem>
    {
        public void Configure(EntityTypeBuilder<ShoppingListItem> builder)
        {
            builder.ToTable("shopping_list");

            builder.HasKey(sli => sli.Id);

            builder.Property(sli => sli.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(sli => sli.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(sli => sli.ShoppingListId)
                .HasColumnName("shopping_list_id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(sli => sli.ProductId)
                .HasColumnName("product_id")
                .HasColumnType("uuid");

            builder.Property(sli => sli.Amount)
                .HasColumnName("amount")
                .HasColumnType("numeric(18,4)")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(sli => sli.Note)
                .HasColumnName("note")
                .HasColumnType("text");

            builder.Property(sli => sli.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(sli => sli.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes
            builder.HasIndex(sli => sli.TenantId)
                .HasDatabaseName("ix_shopping_list_tenant_id");

            builder.HasIndex(sli => sli.ShoppingListId)
                .HasDatabaseName("ix_shopping_list_shopping_list_id");

            builder.HasIndex(sli => sli.ProductId)
                .HasDatabaseName("ix_shopping_list_product_id");

            // Foreign keys
            builder.HasOne(sli => sli.ShoppingList)
                .WithMany(sl => sl.Items)
                .HasForeignKey(sli => sli.ShoppingListId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_shopping_list_shopping_lists");

            builder.HasOne(sli => sli.Product)
                .WithMany()
                .HasForeignKey(sli => sli.ProductId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_shopping_list_products");
        }
    }
}
