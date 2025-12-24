using Famick.HomeManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Famick.HomeManagement.Infrastructure.Configuration
{
    public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
    {
        public void Configure(EntityTypeBuilder<Recipe> builder)
        {
            builder.ToTable("recipes");

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Id)
                .HasColumnName("id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(r => r.TenantId)
                .HasColumnName("tenant_id")
                .HasColumnType("uuid")
                .IsRequired();

            builder.Property(r => r.Name)
                .HasColumnName("name")
                .HasColumnType("character varying(255)")
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(r => r.Description)
                .HasColumnName("description")
                .HasColumnType("text");

            builder.Property(r => r.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(r => r.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Indexes
            builder.HasIndex(r => r.TenantId)
                .HasDatabaseName("ix_recipes_tenant_id");

            builder.HasIndex(r => new { r.TenantId, r.Name })
                .HasDatabaseName("ix_recipes_tenant_name");

            // Navigation properties
            builder.HasMany(r => r.Positions)
                .WithOne(rp => rp.Recipe)
                .HasForeignKey(rp => rp.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.NestedRecipes)
                .WithOne(rn => rn.Recipe)
                .HasForeignKey(rn => rn.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.ParentRecipes)
                .WithOne(rn => rn.IncludedRecipe)
                .HasForeignKey(rn => rn.IncludesRecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
