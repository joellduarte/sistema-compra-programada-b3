using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Application.Interfaces;

public interface ICotacaoService
{
    /// <summary>
    /// Importa cotações de um arquivo COTAHIST da B3.
    /// Retorna a quantidade de registros importados.
    /// </summary>
    Task<int> ImportarArquivoCotahistAsync(string caminhoArquivo);

    /// <summary>
    /// Obtém o preço de fechamento mais recente para um ticker.
    /// </summary>
    Task<decimal?> ObterPrecoFechamentoAsync(string ticker);

    /// <summary>
    /// Obtém todas as cotações de uma data de pregão.
    /// </summary>
    Task<IReadOnlyList<Cotacao>> ObterCotacoesPorDataAsync(DateTime dataPregao);

    /// <summary>
    /// Obtém a data do último pregão importado.
    /// </summary>
    Task<DateTime?> ObterUltimaDataPregaoAsync();
}
