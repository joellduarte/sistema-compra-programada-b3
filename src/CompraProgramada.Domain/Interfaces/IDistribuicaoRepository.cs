using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces;

public interface IDistribuicaoRepository
{
    Task AdicionarAsync(Distribuicao distribuicao);
    Task<IReadOnlyList<Distribuicao>> ObterPorClienteAsync(long clienteId);
}
