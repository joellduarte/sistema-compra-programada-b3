using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class ContaGraficaConfiguration : IEntityTypeConfiguration<ContaGrafica>
{
    public void Configure(EntityTypeBuilder<ContaGrafica> builder)
    {
        builder.ToTable("ContasGraficas");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.ClienteId);

        builder.Property(c => c.NumeroConta)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(c => c.NumeroConta).IsUnique();

        builder.Property(c => c.Tipo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(10);

        builder.Property(c => c.DataCriacao)
            .IsRequired();

        builder.HasMany(c => c.Custodias)
            .WithOne(cu => cu.ContaGrafica)
            .HasForeignKey(cu => cu.ContaGraficaId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed: Conta Master
        builder.HasData(new
        {
            Id = 1L,
            NumeroConta = "MST-000001",
            Tipo = TipoConta.Master,
            DataCriacao = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ClienteId = (long?)null
        });
    }
}
