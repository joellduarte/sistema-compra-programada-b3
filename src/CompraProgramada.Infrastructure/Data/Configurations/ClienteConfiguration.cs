using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("Clientes");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(200);

        builder.OwnsOne(c => c.CPF, cpf =>
        {
            cpf.Property(v => v.Numero)
                .HasColumnName("CPF")
                .IsRequired()
                .HasMaxLength(11)
                .IsFixedLength();

            cpf.HasIndex(v => v.Numero).IsUnique();
        });

        builder.Property(c => c.Email)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.ValorMensal)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(c => c.Ativo)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(c => c.DataAdesao)
            .IsRequired();

        builder.Property(c => c.DataSaida);

        builder.HasOne(c => c.ContaGrafica)
            .WithOne(cg => cg.Cliente)
            .HasForeignKey<ContaGrafica>(cg => cg.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
