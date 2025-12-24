using Famick.HomeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Famick.HomeManagement.Infrastructure.Configuration
{
    public class RecipePositionConfiguration : IEntityTypeConfiguration<RecipePosition>
    {
        public void Configure(EntityTypeBuilder<RecipePosition> builder)
        {
            builder.ToTable("recipes_pos");

            builder.HasKey(rp => rp.Id);

            builder.Property(rp => rp.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(rp => rp.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(rp => rp.RecipeId)
                .HasColumnName("recipe_id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(rp => rp.ProductId)
                .HasColumnName("product_id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(rp => rp.Amount)
                .HasColumnName("amount")
                .HasColumnType("numeric(18,4)")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(rp => rp.Note)
                .HasColumnName("note")
                .HasColumnType("text");

            builder.Property(rp => rp.QuantityUnitId)
                .HasColumnName("qu_id")
                .HasColumnType("uuid");

            builder.Property(rp => rp.OnlyCheckSingleUnitInStock)
                .HasColumnName("only_check_single_unit_in_stock")
                .HasColumnType("smallint")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(rp => rp.IngredientGroup)
                .HasColumnName("ingredient_group")
                .HasColumnType("character varying(255)")
                .HasMaxLength(255);

            builder.Property(rp => rp.NotCheckStockFulfillment)
                .HasColumnName("not_check_stock_fulfillment")
                .HasColumnType("smallint")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(rp => rp.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(rp => rp.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes
            builder.HasIndex(rp => rp.TenantId)
                .HasDatabaseName("ix_recipes_pos_tenant_id");

            builder.HasIndex(rp => rp.RecipeId)
                .HasDatabaseName("ix_recipes_pos_recipe_id");

            builder.HasIndex(rp => rp.ProductId)
                .HasDatabaseName("ix_recipes_pos_product_id");

            // Foreign keys
            builder.HasOne(rp => rp.Recipe)
                .WithMany(r => r.Positions)
                .HasForeignKey(rp => rp.RecipeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_recipes_pos_recipes");

            builder.HasOne(rp => rp.Product)
                .WithMany()
                .HasForeignKey(rp => rp.ProductId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_recipes_pos_products");

            builder.HasOne(rp => rp.QuantityUnit)
                .WithMany()
                .HasForeignKey(rp => rp.QuantityUnitId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("fk_recipes_pos_quantity_units");
        }
    }
}
