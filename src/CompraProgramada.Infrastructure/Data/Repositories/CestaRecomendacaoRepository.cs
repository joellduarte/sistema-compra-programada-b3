using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Data.Repositories;

public class CestaRecomendacaoRepository : ICestaRecomendacaoRepository
{
    private readonly CompraProgramadaDbContext _context;

    public CestaRecomendacaoRepository(CompraProgramadaDbContext context)
    {
        _context = context;
    }

    public async Task<CestaRecomendacao?> ObterAtivaAsync()
    {
        return await _context.CestasRecomendacao
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Ativa);
    }

    public async Task<CestaRecomendacao?> ObterPorIdAsync(long id)
    {
        return await _context.CestasRecomendacao
            .Include(c => c.Itens)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IReadOnlyList<CestaRecomendacao>> ObterHistoricoAsync()
    {
        return await _context.CestasRecomendacao
            .Include(c => c.Itens)
            .OrderByDescending(c => c.DataCriacao)
            .ToListAsync();
    }

    public async Task AdicionarAsync(CestaRecomendacao cesta)
    {
        await _context.CestasRecomendacao.AddAsync(cesta);
    }

    public Task AtualizarAsync(CestaRecomendacao cesta)
    {
        _context.CestasRecomendacao.Update(cesta);
        return Task.CompletedTask;
    }
}
