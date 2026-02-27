using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Data;

public class CompraProgramadaDbContext : DbContext
{
    public CompraProgramadaDbContext(DbContextOptions<CompraProgramadaDbContext> options)
        : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<ContaGrafica> ContasGraficas => Set<ContaGrafica>();
    public DbSet<Custodia> Custodias => Set<Custodia>();
    public DbSet<CestaRecomendacao> CestasRecomendacao => Set<CestaRecomendacao>();
    public DbSet<ItemCesta> ItensCesta => Set<ItemCesta>();
    public DbSet<OrdemCompra> OrdensCompra => Set<OrdemCompra>();
    public DbSet<Distribuicao> Distribuicoes => Set<Distribuicao>();
    public DbSet<EventoIR> EventosIR => Set<EventoIR>();
    public DbSet<Cotacao> Cotacoes => Set<Cotacao>();
    public DbSet<Rebalanceamento> Rebalanceamentos => Set<Rebalanceamento>();
    public DbSet<HistoricoValorMensal> HistoricosValorMensal => Set<HistoricoValorMensal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CompraProgramadaDbContext).Assembly);
    }
}
