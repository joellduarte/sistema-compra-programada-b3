using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class DistribuicaoConfiguration : IEntityTypeConfiguration<Distribuicao>
{
    public void Configure(EntityTypeBuilder<Distribuicao> builder)
    {
        builder.ToTable("Distribuicoes");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedOnAdd();

        builder.Property(d => d.OrdemCompraId)
            .IsRequired();

        builder.Property(d => d.CustodiaFilhoteId)
            .IsRequired();

        builder.Property(d => d.Ticker)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(d => d.Quantidade)
            .IsRequired();

        builder.Property(d => d.PrecoUnitario)
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(d => d.DataDistribuicao)
            .IsRequired();

        builder.HasOne(d => d.CustodiaFilhote)
            .WithMany()
            .HasForeignKey(d => d.CustodiaFilhoteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
