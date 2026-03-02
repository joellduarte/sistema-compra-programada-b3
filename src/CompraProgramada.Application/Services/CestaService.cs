using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;

namespace CompraProgramada.Application.Services;

public class CestaService : ICestaService
{
    private readonly ICestaRecomendacaoRepository _cestaRepository;
    private readonly IContaGraficaRepository _contaGraficaRepository;
    private readonly ICustodiaRepository _custodiaRepository;
    private readonly ICotacaoRepository _cotacaoRepository;
    private readonly IRebalanceamentoService? _rebalanceamentoService;
    private readonly IUnitOfWork _unitOfWork;

    public CestaService(
        ICestaRecomendacaoRepository cestaRepository,
        IContaGraficaRepository contaGraficaRepository,
        ICustodiaRepository custodiaRepository,
        ICotacaoRepository cotacaoRepository,
        IUnitOfWork unitOfWork,
        IRebalanceamentoService? rebalanceamentoService = null)
    {
        _cestaRepository = cestaRepository;
        _contaGraficaRepository = contaGraficaRepository;
        _custodiaRepository = custodiaRepository;
        _cotacaoRepository = cotacaoRepository;
        _unitOfWork = unitOfWork;
        _rebalanceamentoService = rebalanceamentoService;
    }

    public async Task<CestaResponse> CriarCestaAsync(CriarCestaRequest request)
    {
        // RN-014/015/016: Validações de domínio (5 ativos, soma 100%, cada > 0%)
        var itens = request.Itens
            .Select(i => (i.Ticker, i.Percentual))
            .ToList();

        var novaCesta = CestaRecomendacao.Criar(request.Nome, itens);

        // RN-017/018: Desativar cesta anterior na mesma transação
        var cestaAnterior = await _cestaRepository.ObterAtivaAsync();
        if (cestaAnterior is not null)
        {
            cestaAnterior.Desativar();
            await _cestaRepository.AtualizarAsync(cestaAnterior);
        }

        await _cestaRepository.AdicionarAsync(novaCesta);
        await _unitOfWork.CommitAsync();

        // RN-019: Disparar rebalanceamento automático se havia cesta anterior
        if (cestaAnterior is not null && _rebalanceamentoService is not null)
        {
            await _rebalanceamentoService.RebalancearPorMudancaCestaAsync(
                cestaAnterior.Id, novaCesta.Id);
        }

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

    public async Task<CustodiaMasterResponse> ConsultarCustodiaMasterAsync()
    {
        var contaMaster = await _contaGraficaRepository.ObterMasterAsync()
            ?? throw new InvalidOperationException("Conta master não encontrada.");

        var custodias = await _custodiaRepository.ObterPorContaGraficaIdAsync(contaMaster.Id);

        var ativos = new List<AtivoMasterDto>();
        decimal valorTotal = 0;

        foreach (var custodia in custodias.Where(c => c.Quantidade > 0))
        {
            var cotacao = await _cotacaoRepository.ObterUltimaFechamentoAsync(custodia.Ticker);
            var precoAtual = cotacao?.PrecoFechamento ?? custodia.PrecoMedio;
            var valorAtual = custodia.Quantidade * precoAtual;
            valorTotal += valorAtual;

            ativos.Add(new AtivoMasterDto(
                custodia.Ticker,
                custodia.Quantidade,
                Math.Round(custodia.PrecoMedio, 2),
                Math.Round(valorAtual, 2)));
        }

        return new CustodiaMasterResponse(
            new ContaMasterDto(contaMaster.Id, contaMaster.NumeroConta, "MASTER"),
            ativos,
            Math.Round(valorTotal, 2));
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
