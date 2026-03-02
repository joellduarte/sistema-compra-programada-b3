using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces;

public interface ICotacaoRepository
{
    Task<Cotacao?> ObterUltimaFechamentoAsync(string ticker);
    Task<IReadOnlyList<Cotacao>> ObterPorDataAsync(DateTime dataPregao);
    Task AdicionarVariasAsync(IEnumerable<Cotacao> cotacoes);
    Task<int> UpsertVariasAsync(IEnumerable<Cotacao> cotacoes);
    Task<DateTime?> ObterUltimaDataPregaoAsync();
}
