using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces;

public interface ICestaRecomendacaoRepository
{
    Task<CestaRecomendacao?> ObterAtivaAsync();
    Task<CestaRecomendacao?> ObterPorIdAsync(long id);
    Task<IReadOnlyList<CestaRecomendacao>> ObterHistoricoAsync();
    Task AdicionarAsync(CestaRecomendacao cesta);
    Task AtualizarAsync(CestaRecomendacao cesta);
}
