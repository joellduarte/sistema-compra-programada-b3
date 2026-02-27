using CompraProgramada.Domain.Interfaces;

namespace CompraProgramada.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly CompraProgramadaDbContext _context;

    public UnitOfWork(CompraProgramadaDbContext context)
    {
        _context = context;
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
