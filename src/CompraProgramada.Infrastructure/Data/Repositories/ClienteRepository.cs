using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Data.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly CompraProgramadaDbContext _context;

    public ClienteRepository(CompraProgramadaDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> ObterPorIdAsync(long id)
    {
        return await _context.Clientes.FindAsync(id);
    }

    public async Task<Cliente?> ObterPorCpfAsync(string cpf)
    {
        var apenasDigitos = new string(cpf?.Where(char.IsDigit).ToArray() ?? []);
        return await _context.Clientes
            .FirstOrDefaultAsync(c => c.CPF.Numero == apenasDigitos);
    }

    public async Task<IReadOnlyList<Cliente>> ObterAtivosAsync()
    {
        return await _context.Clientes
            .Where(c => c.Ativo)
            .ToListAsync();
    }

    public async Task AdicionarAsync(Cliente cliente)
    {
        await _context.Clientes.AddAsync(cliente);
    }

    public Task AtualizarAsync(Cliente cliente)
    {
        _context.Clientes.Update(cliente);
        return Task.CompletedTask;
    }
}
