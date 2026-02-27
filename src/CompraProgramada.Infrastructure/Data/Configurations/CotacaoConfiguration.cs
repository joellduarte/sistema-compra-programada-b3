using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class CotacaoConfiguration : IEntityTypeConfiguration<Cotacao>
{
    public void Configure(EntityTypeBuilder<Cotacao> builder)
    {
        builder.ToTable("Cotacoes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.DataPregao)
            .IsRequired();

        builder.Property(c => c.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(c => c.CodigoBDI)
            .IsRequired()
            .HasMaxLength(2);

        builder.Property(c => c.TipoMercado)
            .IsRequired();

        builder.Property(c => c.NomeEmpresa)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.PrecoAbertura)
            .HasColumnType("decimal(18,4)");

        builder.Property(c => c.PrecoFechamento)
            .HasColumnType("decimal(18,4)");

        builder.Property(c => c.PrecoMaximo)
            .HasColumnType("decimal(18,4)");

        builder.Property(c => c.PrecoMinimo)
            .HasColumnType("decimal(18,4)");

        builder.Property(c => c.PrecoMedio)
            .HasColumnType("decimal(18,4)");

        builder.Property(c => c.QuantidadeNegociada);

        builder.Property(c => c.VolumeNegociado)
            .HasColumnType("decimal(18,2)");

        // Índice único: um registro por ticker/data/tipo mercado
        builder.HasIndex(c => new { c.DataPregao, c.Ticker, c.TipoMercado }).IsUnique();

        // Índice para busca rápida por ticker
        builder.HasIndex(c => c.Ticker);
        builder.HasIndex(c => c.DataPregao);
    }
}
