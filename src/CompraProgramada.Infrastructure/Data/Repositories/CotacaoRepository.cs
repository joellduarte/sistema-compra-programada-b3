using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Data.Repositories;

public class CotacaoRepository : ICotacaoRepository
{
    private readonly CompraProgramadaDbContext _context;

    public CotacaoRepository(CompraProgramadaDbContext context)
    {
        _context = context;
    }

    public async Task<Cotacao?> ObterUltimaFechamentoAsync(string ticker)
    {
        return await _context.Cotacoes
            .Where(c => c.Ticker == ticker)
            .OrderByDescending(c => c.DataPregao)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<Cotacao>> ObterPorDataAsync(DateTime dataPregao)
    {
        return await _context.Cotacoes
            .Where(c => c.DataPregao == dataPregao.Date)
            .ToListAsync();
    }

    public async Task AdicionarVariasAsync(IEnumerable<Cotacao> cotacoes)
    {
        await _context.Cotacoes.AddRangeAsync(cotacoes);
    }

    public async Task<DateTime?> ObterUltimaDataPregaoAsync()
    {
        if (!await _context.Cotacoes.AnyAsync())
            return null;

        return await _context.Cotacoes
            .MaxAsync(c => c.DataPregao);
    }
}
