using CompraProgramada.Application.DTOs;

namespace CompraProgramada.Application.Interfaces;

public interface IRebalanceamentoService
{
    /// <summary>
    /// RN-045 a RN-049: Rebalanceia todos os clientes após mudança de cesta.
    /// </summary>
    Task<RebalanceamentoResponse> RebalancearPorMudancaCestaAsync(
        long cestaAnteriorId, long cestaNovaId);

    /// <summary>
    /// RN-050 a RN-052: Rebalanceia um cliente por desvio de proporção (limiar 5pp).
    /// </summary>
    Task<RebalanceamentoResponse> RebalancearPorDesvioAsync(decimal limiarPercentual = 5m);
}
