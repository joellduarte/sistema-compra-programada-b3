using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Data.Repositories;

public class DistribuicaoRepository : IDistribuicaoRepository
{
    private readonly CompraProgramadaDbContext _context;

    public DistribuicaoRepository(CompraProgramadaDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(Distribuicao distribuicao)
    {
        await _context.Distribuicoes.AddAsync(distribuicao);
    }

    public async Task<IReadOnlyList<Distribuicao>> ObterPorClienteAsync(long clienteId)
    {
        return await _context.Distribuicoes
            .Include(d => d.OrdemCompra)
            .Where(d => d.CustodiaFilhote.ContaGrafica.ClienteId == clienteId)
            .OrderByDescending(d => d.DataDistribuicao)
            .ToListAsync();
    }
}
