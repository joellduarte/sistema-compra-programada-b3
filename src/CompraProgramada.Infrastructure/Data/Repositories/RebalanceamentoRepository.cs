using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Data.Repositories;

public class RebalanceamentoRepository : IRebalanceamentoRepository
{
    private readonly CompraProgramadaDbContext _context;

    public RebalanceamentoRepository(CompraProgramadaDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(Rebalanceamento rebalanceamento)
    {
        await _context.Rebalanceamentos.AddAsync(rebalanceamento);
    }

    public async Task<decimal> ObterTotalVendasClienteNoMesAsync(long clienteId, int ano, int mes)
    {
        return await _context.Rebalanceamentos
            .Where(r => r.ClienteId == clienteId
                && r.DataRebalanceamento.Year == ano
                && r.DataRebalanceamento.Month == mes
                && r.QuantidadeVendida > 0)
            .SumAsync(r => r.ValorVenda);
    }
}
