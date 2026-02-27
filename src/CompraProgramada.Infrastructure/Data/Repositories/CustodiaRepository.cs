using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Data.Repositories;

public class CustodiaRepository : ICustodiaRepository
{
    private readonly CompraProgramadaDbContext _context;

    public CustodiaRepository(CompraProgramadaDbContext context)
    {
        _context = context;
    }

    public async Task<Custodia?> ObterPorContaETickerAsync(long contaGraficaId, string ticker)
    {
        return await _context.Custodias
            .FirstOrDefaultAsync(c =>
                c.ContaGraficaId == contaGraficaId &&
                c.Ticker == ticker.ToUpperInvariant());
    }

    public async Task<IReadOnlyList<Custodia>> ObterPorContaGraficaIdAsync(long contaGraficaId)
    {
        return await _context.Custodias
            .Where(c => c.ContaGraficaId == contaGraficaId)
            .ToListAsync();
    }

    public async Task AdicionarAsync(Custodia custodia)
    {
        await _context.Custodias.AddAsync(custodia);
    }

    public Task AtualizarAsync(Custodia custodia)
    {
        _context.Custodias.Update(custodia);
        return Task.CompletedTask;
    }
}
