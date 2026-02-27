using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class HistoricoValorMensalConfiguration : IEntityTypeConfiguration<HistoricoValorMensal>
{
    public void Configure(EntityTypeBuilder<HistoricoValorMensal> builder)
    {
        builder.ToTable("HistoricosValorMensal");

        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedOnAdd();

        builder.Property(h => h.ClienteId)
            .IsRequired();

        builder.Property(h => h.ValorAnterior)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(h => h.ValorNovo)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(h => h.DataAlteracao)
            .IsRequired();

        builder.HasOne(h => h.Cliente)
            .WithMany()
            .HasForeignKey(h => h.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(h => h.ClienteId);
    }
}
