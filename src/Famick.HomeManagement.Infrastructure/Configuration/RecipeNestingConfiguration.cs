using Famick.HomeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Famick.HomeManagement.Infrastructure.Configuration
{
    public class RecipeNestingConfiguration : IEntityTypeConfiguration<RecipeNesting>
    {
        public void Configure(EntityTypeBuilder<RecipeNesting> builder)
        {
            builder.ToTable("recipes_nestings");

            builder.HasKey(rn => rn.Id);

            builder.Property(rn => rn.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(rn => rn.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(rn => rn.RecipeId)
                .HasColumnName("recipe_id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(rn => rn.IncludesRecipeId)
                .HasColumnName("includes_recipe_id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(rn => rn.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(rn => rn.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes
            builder.HasIndex(rn => rn.TenantId)
                .HasDatabaseName("ix_recipes_nestings_tenant_id");

            // Unique constraint to prevent duplicate recipe nestings
            builder.HasIndex(rn => new { rn.RecipeId, rn.IncludesRecipeId })
                .IsUnique()
                .HasDatabaseName("ux_recipes_nestings_recipe_includes");

            // Foreign keys (self-referential to recipes table)
            builder.HasOne(rn => rn.Recipe)
                .WithMany(r => r.NestedRecipes)
                .HasForeignKey(rn => rn.RecipeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_recipes_nestings_recipe_id");

            builder.HasOne(rn => rn.IncludedRecipe)
                .WithMany(r => r.ParentRecipes)
                .HasForeignKey(rn => rn.IncludesRecipeId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_recipes_nestings_includes_recipe_id");
        }
    }
}
