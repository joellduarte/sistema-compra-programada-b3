using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;

namespace CompraProgramada.Application.Services;

public class CotacaoService : ICotacaoService
{
    private readonly ICotacaoRepository _cotacaoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICotahistParser _parser;

    public CotacaoService(
        ICotacaoRepository cotacaoRepository,
        IUnitOfWork unitOfWork,
        ICotahistParser parser)
    {
        _cotacaoRepository = cotacaoRepository;
        _unitOfWork = unitOfWork;
        _parser = parser;
    }

    public async Task<int> ImportarArquivoCotahistAsync(string caminhoArquivo)
    {
        var cotacoes = _parser.ParseArquivo(caminhoArquivo);

        if (cotacoes.Count == 0)
            return 0;

        await _cotacaoRepository.AdicionarVariasAsync(cotacoes);
        await _unitOfWork.CommitAsync();

        return cotacoes.Count;
    }

    public async Task<decimal?> ObterPrecoFechamentoAsync(string ticker)
    {
        var cotacao = await _cotacaoRepository.ObterUltimaFechamentoAsync(ticker);
        return cotacao?.PrecoFechamento;
    }

    public async Task<IReadOnlyList<Cotacao>> ObterCotacoesPorDataAsync(DateTime dataPregao)
    {
        return await _cotacaoRepository.ObterPorDataAsync(dataPregao);
    }

    public async Task<DateTime?> ObterUltimaDataPregaoAsync()
    {
        return await _cotacaoRepository.ObterUltimaDataPregaoAsync();
    }
}
