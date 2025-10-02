using Backend.Domain.Entities;
using Backend.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.ToTable("blocks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(b => b.PageId)
            .HasColumnName("page_id")
            .IsRequired();

        builder.Property(b => b.ParentBlockId)
            .HasColumnName("parent_block_id");

        builder.Property(b => b.Json)
            .HasColumnName("json")
            .HasColumnType("jsonb");

        // SortKey value object mapping - stored as decimal(18,9)
        builder.Property(b => b.SortKey)
            .HasColumnName("sort_key")
            .IsRequired()
            .HasColumnType("decimal(18,9)")
            .HasConversion(
                sortKey => sortKey.Value,
                value => SortKey.Create(value));

        // BlockType value object mapping
        builder.Property(b => b.Type)
            .HasColumnName("type")
            .IsRequired()
            .HasMaxLength(50)
            .HasConversion(
                type => type.Value,
                value => BlockType.Create(value));

        builder.HasIndex(b => b.PageId);
        builder.HasIndex(b => b.ParentBlockId);
        builder.HasIndex(b => new { b.PageId, b.ParentBlockId, b.SortKey });
    }
}
