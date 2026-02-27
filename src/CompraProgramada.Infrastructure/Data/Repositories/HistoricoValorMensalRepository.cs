using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;

namespace CompraProgramada.Infrastructure.Data.Repositories;

public class HistoricoValorMensalRepository : IHistoricoValorMensalRepository
{
    private readonly CompraProgramadaDbContext _context;

    public HistoricoValorMensalRepository(CompraProgramadaDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(HistoricoValorMensal historico)
    {
        await _context.HistoricosValorMensal.AddAsync(historico);
    }
}
