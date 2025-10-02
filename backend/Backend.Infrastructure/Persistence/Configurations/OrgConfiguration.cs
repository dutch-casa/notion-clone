using Backend.Domain.Aggregates;
using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class OrgConfiguration : IEntityTypeConfiguration<Org>
{
    public void Configure(EntityTypeBuilder<Org> builder)
    {
        builder.ToTable("orgs");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(o => o.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Members collection - configure backing field access
        builder.HasMany(o => o.Members)
            .WithOne()
            .HasForeignKey(m => m.OrgId)
            .OnDelete(DeleteBehavior.Cascade);

        // Tell EF Core to use the private backing field _members for the Members property
        builder.Navigation(o => o.Members)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(o => o.Name);
    }
}
