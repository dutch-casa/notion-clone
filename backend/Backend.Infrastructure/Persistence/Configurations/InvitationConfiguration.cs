using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class InvitationConfiguration : IEntityTypeConfiguration<Invitation>
{
    public void Configure(EntityTypeBuilder<Invitation> builder)
    {
        builder.ToTable("invitations");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(i => i.OrgId)
            .HasColumnName("org_id")
            .IsRequired();

        builder.Property(i => i.InvitedUserId)
            .HasColumnName("invited_user_id")
            .IsRequired();

        builder.Property(i => i.InviterUserId)
            .HasColumnName("inviter_user_id")
            .IsRequired();

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion<int>();

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(i => i.RespondedAt)
            .HasColumnName("responded_at");

        // OrgRole value object mapping
        builder.Property(i => i.Role)
            .HasColumnName("role")
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(
                role => role.Value,
                value => OrgRole.Create(value));

        // Indexes for efficient querying
        builder.HasIndex(i => i.OrgId);
        builder.HasIndex(i => i.InvitedUserId);
        builder.HasIndex(i => new { i.Status, i.InvitedUserId });
    }
}
