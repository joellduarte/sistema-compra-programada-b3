using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class EventoIRConfiguration : IEntityTypeConfiguration<EventoIR>
{
    public void Configure(EntityTypeBuilder<EventoIR> builder)
    {
        builder.ToTable("EventosIR");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.ClienteId)
            .IsRequired();

        builder.Property(e => e.Tipo)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(15);

        builder.Property(e => e.Ticker)
            .HasMaxLength(10);

        builder.Property(e => e.ValorBase)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.Aliquota)
            .HasColumnType("decimal(10,6)")
            .IsRequired();

        builder.Property(e => e.ValorIR)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(e => e.PublicadoKafka)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.DataEvento)
            .IsRequired();

        builder.HasOne(e => e.Cliente)
            .WithMany()
            .HasForeignKey(e => e.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.ClienteId);
        builder.HasIndex(e => e.PublicadoKafka);
    }
}
