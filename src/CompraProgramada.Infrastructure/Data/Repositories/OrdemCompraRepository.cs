using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Data.Repositories;

public class OrdemCompraRepository : IOrdemCompraRepository
{
    private readonly CompraProgramadaDbContext _context;

    public OrdemCompraRepository(CompraProgramadaDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(OrdemCompra ordem)
    {
        await _context.OrdensCompra.AddAsync(ordem);
    }

    public async Task<bool> ExisteParaDataAsync(DateTime dataReferencia)
    {
        return await _context.OrdensCompra
            .AnyAsync(o => o.DataReferencia.Date == dataReferencia.Date);
    }
}
