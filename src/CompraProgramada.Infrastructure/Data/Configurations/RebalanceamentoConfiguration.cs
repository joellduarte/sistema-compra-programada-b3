using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class RebalanceamentoConfiguration : IEntityTypeConfiguration<Rebalanceamento>
{
    public void Configure(EntityTypeBuilder<Rebalanceamento> builder)
    {
        builder.ToTable("Rebalanceamentos");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();

        builder.Property(r => r.ClienteId)
            .IsRequired();

        builder.Property(r => r.Tipo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.TickerVendido)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(r => r.QuantidadeVendida)
            .IsRequired();

        builder.Property(r => r.PrecoVenda)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(r => r.ValorVenda)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(r => r.TickerComprado)
            .HasMaxLength(10);

        builder.Property(r => r.QuantidadeComprada);

        builder.Property(r => r.PrecoCompra)
            .HasColumnType("decimal(18,4)");

        builder.Property(r => r.ValorCompra)
            .HasColumnType("decimal(18,2)");

        builder.Property(r => r.DataRebalanceamento)
            .IsRequired();

        builder.HasOne(r => r.Cliente)
            .WithMany()
            .HasForeignKey(r => r.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.ClienteId);
        builder.HasIndex(r => r.DataRebalanceamento);
    }
}
