using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class OrdemCompraConfiguration : IEntityTypeConfiguration<OrdemCompra>
{
    public void Configure(EntityTypeBuilder<OrdemCompra> builder)
    {
        builder.ToTable("OrdensCompra");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd();

        builder.Property(o => o.ContaMasterId)
            .IsRequired();

        builder.Property(o => o.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(o => o.Quantidade)
            .IsRequired();

        builder.Property(o => o.PrecoUnitario)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(o => o.TipoMercado)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(15);

        builder.Property(o => o.DataExecucao)
            .IsRequired();

        builder.Property(o => o.DataReferencia)
            .IsRequired();

        builder.HasOne(o => o.ContaMaster)
            .WithMany()
            .HasForeignKey(o => o.ContaMasterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(o => o.Distribuicoes)
            .WithOne(d => d.OrdemCompra)
            .HasForeignKey(d => d.OrdemCompraId)
            .OnDelete(DeleteBehavior.Cascade);

        // Índice para verificar compra duplicada por data
        builder.HasIndex(o => o.DataReferencia);
    }
}
