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

    private const int TamanhoBatch = 5000;

    public async Task<(int total, int inseridos, int atualizados)> ImportarArquivoCotahistAsync(string caminhoArquivo)
    {
        var cotacoes = _parser.ParseArquivo(caminhoArquivo);
        return await SalvarEmLotesComUpsertAsync(cotacoes);
    }

    public async Task<(int total, int inseridos, int atualizados)> ImportarStreamCotahistAsync(Stream stream)
    {
        var cotacoes = _parser.ParseStream(stream);
        return await SalvarEmLotesComUpsertAsync(cotacoes);
    }

    private async Task<(int total, int inseridos, int atualizados)> SalvarEmLotesComUpsertAsync(IReadOnlyList<Cotacao> cotacoes)
    {
        if (cotacoes.Count == 0)
            return (0, 0, 0);

        var totalInseridos = 0;

        for (var i = 0; i < cotacoes.Count; i += TamanhoBatch)
        {
            var lote = cotacoes.Skip(i).Take(TamanhoBatch);
            totalInseridos += await _cotacaoRepository.UpsertVariasAsync(lote);
            await _unitOfWork.CommitAsync();
        }

        var totalAtualizados = cotacoes.Count - totalInseridos;
        return (cotacoes.Count, totalInseridos, totalAtualizados);
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
