using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces;

public interface ICustodiaRepository
{
    Task<Custodia?> ObterPorContaETickerAsync(long contaGraficaId, string ticker);
    Task<IReadOnlyList<Custodia>> ObterPorContaGraficaIdAsync(long contaGraficaId);
    Task AdicionarAsync(Custodia custodia);
    Task AtualizarAsync(Custodia custodia);
}
