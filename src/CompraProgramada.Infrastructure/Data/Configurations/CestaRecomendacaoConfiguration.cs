using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompraProgramada.Infrastructure.Data.Configurations;

public class CestaRecomendacaoConfiguration : IEntityTypeConfiguration<CestaRecomendacao>
{
    public void Configure(EntityTypeBuilder<CestaRecomendacao> builder)
    {
        builder.ToTable("CestasRecomendacao");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.Nome)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Ativa)
            .IsRequired();

        builder.Property(c => c.DataCriacao)
            .IsRequired();

        builder.Property(c => c.DataDesativacao);

        builder.HasMany(c => c.Itens)
            .WithOne(i => i.Cesta)
            .HasForeignKey(i => i.CestaId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
