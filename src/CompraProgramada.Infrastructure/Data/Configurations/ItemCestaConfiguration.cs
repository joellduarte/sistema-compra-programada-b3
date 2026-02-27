using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class ItemCestaConfiguration : IEntityTypeConfiguration<ItemCesta>
{
    public void Configure(EntityTypeBuilder<ItemCesta> builder)
    {
        builder.ToTable("ItensCesta");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedOnAdd();

        builder.Property(i => i.CestaId)
            .IsRequired();

        builder.Property(i => i.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(i => i.Percentual)
            .HasColumnType("decimal(5,2)")
            .IsRequired();
    }
}
