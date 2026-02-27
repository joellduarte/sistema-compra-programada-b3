using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class CustodiaConfiguration : IEntityTypeConfiguration<Custodia>
{
    public void Configure(EntityTypeBuilder<Custodia> builder)
    {
        builder.ToTable("Custodias");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.ContaGraficaId)
            .IsRequired();

        builder.Property(c => c.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.Quantidade)
            .IsRequired();

        builder.Property(c => c.PrecoMedio)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(c => c.DataUltimaAtualizacao)
            .IsRequired();

        // Índice único: uma posição por ticker por conta
        builder.HasIndex(c => new { c.ContaGraficaId, c.Ticker }).IsUnique();
    }
}
