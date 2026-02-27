using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;

namespace CompraProgramada.Application.Services;

public class CestaService : ICestaService
{
    private readonly ICestaRecomendacaoRepository _cestaRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CestaService(
        ICestaRecomendacaoRepository cestaRepository,
        IUnitOfWork unitOfWork)
    {
        _cestaRepository = cestaRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CestaResponse> CriarCestaAsync(CriarCestaRequest request)
    {
        // RN-014/015/016: Validações de domínio (5 ativos, soma 100%, cada > 0%)
        var itens = request.Itens
            .Select(i => (i.Ticker, i.Percentual))
            .ToList();

        var novaCesta = CestaRecomendacao.Criar(request.Nome, itens);

        // RN-017/018: Desativar cesta anterior na mesma transação
        var cestaAtiva = await _cestaRepository.ObterAtivaAsync();
        if (cestaAtiva is not null)
        {
            cestaAtiva.Desativar();
            await _cestaRepository.AtualizarAsync(cestaAtiva);
        }

        await _cestaRepository.AdicionarAsync(novaCesta);
        await _unitOfWork.CommitAsync();

        return MapToResponse(novaCesta);
    }

    public async Task<CestaResponse?> ObterCestaAtivaAsync()
    {
        var cesta = await _cestaRepository.ObterAtivaAsync();
        return cesta is null ? null : MapToResponse(cesta);
    }

    public async Task<CestaResponse?> ObterCestaPorIdAsync(long id)
    {
        var cesta = await _cestaRepository.ObterPorIdAsync(id);
        return cesta is null ? null : MapToResponse(cesta);
    }

    public async Task<IReadOnlyList<CestaResponse>> ObterHistoricoAsync()
    {
        var cestas = await _cestaRepository.ObterHistoricoAsync();
        return cestas.Select(MapToResponse).ToList();
    }

    private static CestaResponse MapToResponse(CestaRecomendacao cesta) =>
        new(
            cesta.Id,
            cesta.Nome,
            cesta.Ativa,
            cesta.DataCriacao,
            cesta.DataDesativacao,
            cesta.Itens.Select(i => new ItemCestaResponse(i.Ticker, i.Percentual)).ToList());
}
