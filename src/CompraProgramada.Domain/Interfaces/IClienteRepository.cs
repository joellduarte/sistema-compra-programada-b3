using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces;

public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(long id);
    Task<Cliente?> ObterPorCpfAsync(string cpf);
    Task<IReadOnlyList<Cliente>> ObterAtivosAsync();
    Task AdicionarAsync(Cliente cliente);
    Task AtualizarAsync(Cliente cliente);
}
