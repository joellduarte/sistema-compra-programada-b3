using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces;

public interface IRebalanceamentoRepository
{
    Task AdicionarAsync(Rebalanceamento rebalanceamento);
    Task<decimal> ObterTotalVendasClienteNoMesAsync(long clienteId, int ano, int mes);
}
