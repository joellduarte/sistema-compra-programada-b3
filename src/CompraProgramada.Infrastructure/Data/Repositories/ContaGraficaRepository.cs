using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Data.Repositories;

public class ContaGraficaRepository : IContaGraficaRepository
{
    private readonly CompraProgramadaDbContext _context;

    public ContaGraficaRepository(CompraProgramadaDbContext context)
    {
        _context = context;
    }

    public async Task<ContaGrafica?> ObterMasterAsync()
    {
        return await _context.ContasGraficas
            .FirstOrDefaultAsync(c => c.Tipo == TipoConta.Master);
    }

    public async Task<ContaGrafica?> ObterPorClienteIdAsync(long clienteId)
    {
        return await _context.ContasGraficas
            .FirstOrDefaultAsync(c => c.ClienteId == clienteId);
    }

    public async Task AdicionarAsync(ContaGrafica contaGrafica)
    {
        await _context.ContasGraficas.AddAsync(contaGrafica);
    }

    public async Task<long> ObterProximoNumeroContaAsync()
    {
        var ultimaConta = await _context.ContasGraficas
            .Where(c => c.Tipo == TipoConta.Filhote)
            .OrderByDescending(c => c.Id)
            .FirstOrDefaultAsync();

        return (ultimaConta?.Id ?? 0) + 1;
    }
}
