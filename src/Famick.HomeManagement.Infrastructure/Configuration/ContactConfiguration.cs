using Famick.HomeManagement.Domain.Entities;
using Famick.HomeManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Famick.HomeManagement.Infrastructure.Configuration;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToTable("contacts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.TenantId)
            .IsRequired();

        builder.HasIndex(c => c.TenantId);

        // Name fields
        builder.Property(c => c.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.MiddleName)
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.PreferredName)
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .HasMaxLength(255);

        // Demographics
        builder.Property(c => c.Gender)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(Gender.Unknown);

        // Birth date precision
        builder.Property(c => c.BirthDatePrecision)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(DatePrecision.Unknown);

        // Death date precision
        builder.Property(c => c.DeathDatePrecision)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(DatePrecision.Unknown);

        builder.Property(c => c.Notes)
            .HasMaxLength(4000);

        // Visibility
        builder.Property(c => c.Visibility)
            .HasConversion<string>()
            .HasMaxLength(50)
            .HasDefaultValue(ContactVisibilityLevel.TenantShared);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.UsesTenantAddress)
            .IsRequired()
            .HasDefaultValue(false);

        // Timestamps
        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        // Indexes for search
        builder.HasIndex(c => new { c.TenantId, c.FirstName, c.LastName });
        builder.HasIndex(c => new { c.TenantId, c.Email });
        builder.HasIndex(c => new { c.TenantId, c.LinkedUserId });
        builder.HasIndex(c => new { c.TenantId, c.Visibility });
        builder.HasIndex(c => new { c.TenantId, c.IsActive });

        // FK to CreatedByUser
        builder.HasOne(c => c.CreatedByUser)
            .WithMany()
            .HasForeignKey(c => c.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore computed properties
        builder.Ignore(c => c.DisplayName);
        builder.Ignore(c => c.FullName);
    }
}
