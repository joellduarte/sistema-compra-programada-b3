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

    public async Task<int> UpsertVariasAsync(IEnumerable<Cotacao> cotacoes)
    {
        var lista = cotacoes.ToList();
        if (lista.Count == 0) return 0;

        var datas = lista.Select(c => c.DataPregao.Date).Distinct().ToList();
        var existentes = await _context.Cotacoes
            .Where(c => datas.Contains(c.DataPregao))
            .ToListAsync();

        var existenteMap = existentes
            .ToDictionary(c => (c.DataPregao.Date, c.Ticker, c.TipoMercado));

        var inseridos = 0;
        foreach (var cotacao in lista)
        {
            var chave = (cotacao.DataPregao.Date, cotacao.Ticker, cotacao.TipoMercado);
            if (existenteMap.TryGetValue(chave, out var existente))
            {
                existente.Atualizar(
                    cotacao.PrecoAbertura, cotacao.PrecoFechamento,
                    cotacao.PrecoMaximo, cotacao.PrecoMinimo,
                    cotacao.PrecoMedio, cotacao.QuantidadeNegociada,
                    cotacao.VolumeNegociado);
            }
            else
            {
                await _context.Cotacoes.AddAsync(cotacao);
                existenteMap[chave] = cotacao;
                inseridos++;
            }
        }

        return inseridos;
    }

    public async Task<DateTime?> ObterUltimaDataPregaoAsync()
    {
        if (!await _context.Cotacoes.AnyAsync())
            return null;

        return await _context.Cotacoes
            .MaxAsync(c => c.DataPregao);
    }
}
