using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(m => m.OrgId)
            .HasColumnName("org_id")
            .IsRequired();

        builder.Property(m => m.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(m => m.JoinedAt)
            .HasColumnName("joined_at")
            .IsRequired();

        // OrgRole value object mapping
        builder.Property(m => m.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(
                role => role.Value,
                value => OrgRole.Create(value));

        builder.HasIndex(m => new { m.OrgId, m.UserId })
            .IsUnique();

        builder.HasIndex(m => m.UserId);
    }
}
