using CompraProgramada.Application.DTOs;

namespace CompraProgramada.Application.Interfaces;

public interface ICestaService
{
    Task<CestaResponse> CriarCestaAsync(CriarCestaRequest request);
    Task<CestaResponse?> ObterCestaAtivaAsync();
    Task<CestaResponse?> ObterCestaPorIdAsync(long id);
    Task<IReadOnlyList<CestaResponse>> ObterHistoricoAsync();
}
