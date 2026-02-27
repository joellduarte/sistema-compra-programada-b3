using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces;

public interface IContaGraficaRepository
{
    Task<ContaGrafica?> ObterMasterAsync();
    Task<ContaGrafica?> ObterPorClienteIdAsync(long clienteId);
    Task AdicionarAsync(ContaGrafica contaGrafica);
    Task<long> ObterProximoNumeroContaAsync();
}
