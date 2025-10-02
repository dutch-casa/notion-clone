using Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("pages");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(p => p.OrgId)
            .HasColumnName("org_id")
            .IsRequired();

        builder.Property(p => p.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(p => p.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Blocks collection - no inverse navigation on Block (respects aggregate boundaries)
        builder.HasMany(p => p.Blocks)
            .WithOne()
            .HasForeignKey(b => b.PageId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tell EF Core to use the private backing field _blocks for the Blocks property
        builder.Navigation(p => p.Blocks)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => p.OrgId);
        builder.HasIndex(p => p.CreatedBy);
        builder.HasIndex(p => p.Title);
    }
}
