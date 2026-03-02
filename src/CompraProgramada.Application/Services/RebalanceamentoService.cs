using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using CompraProgramada.Domain.Interfaces;

namespace CompraProgramada.Application.Services;

public class RebalanceamentoService : IRebalanceamentoService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IContaGraficaRepository _contaGraficaRepository;
    private readonly ICustodiaRepository _custodiaRepository;
    private readonly ICotacaoRepository _cotacaoRepository;
    private readonly ICestaRecomendacaoRepository _cestaRepository;
    private readonly IRebalanceamentoRepository _rebalanceamentoRepository;
    private readonly IEventoIRService _eventoIRService;
    private readonly IUnitOfWork _unitOfWork;

    public RebalanceamentoService(
        IClienteRepository clienteRepository,
        IContaGraficaRepository contaGraficaRepository,
        ICustodiaRepository custodiaRepository,
        ICotacaoRepository cotacaoRepository,
        ICestaRecomendacaoRepository cestaRepository,
        IRebalanceamentoRepository rebalanceamentoRepository,
        IEventoIRService eventoIRService,
        IUnitOfWork unitOfWork)
    {
        _clienteRepository = clienteRepository;
        _contaGraficaRepository = contaGraficaRepository;
        _custodiaRepository = custodiaRepository;
        _cotacaoRepository = cotacaoRepository;
        _cestaRepository = cestaRepository;
        _rebalanceamentoRepository = rebalanceamentoRepository;
        _eventoIRService = eventoIRService;
        _unitOfWork = unitOfWork;
    }

    public async Task<RebalanceamentoResponse> RebalancearPorMudancaCestaAsync(
        long cestaAnteriorId, long cestaNovaId)
    {
        var cestaAnterior = await _cestaRepository.ObterPorIdAsync(cestaAnteriorId)
            ?? throw new KeyNotFoundException("Cesta anterior não encontrada.");
        var cestaNova = await _cestaRepository.ObterPorIdAsync(cestaNovaId)
            ?? throw new KeyNotFoundException("Nova cesta não encontrada.");

        var tickersAnteriores = cestaAnterior.ObterTickers();
        var tickersNovos = cestaNova.ObterTickers();

        // RN-046: Ativos que saíram e entraram
        var tickersSairam = tickersAnteriores.Except(tickersNovos).ToHashSet();
        var tickersEntraram = tickersNovos.Except(tickersAnteriores).ToHashSet();
        var tickersPermaneceram = tickersAnteriores.Intersect(tickersNovos).ToHashSet();

        var clientesAtivos = await _clienteRepository.ObterAtivosAsync();
        var detalhes = new List<RebalanceamentoClienteDto>();

        foreach (var cliente in clientesAtivos)
        {
            var contaFilhote = await _contaGraficaRepository.ObterPorClienteIdAsync(cliente.Id);
            if (contaFilhote is null) continue;

            var custodias = await _custodiaRepository.ObterPorContaGraficaIdAsync(contaFilhote.Id);
            var vendas = new List<OperacaoRebalanceamentoDto>();
            var compras = new List<OperacaoRebalanceamentoDto>();
            decimal totalVendas = 0;
            decimal lucroLiquido = 0;

            // RN-047: Vender toda posição dos ativos que saíram
            foreach (var ticker in tickersSairam)
            {
                var custodia = custodias.FirstOrDefault(c => c.Ticker == ticker);
                if (custodia is null || custodia.Quantidade == 0) continue;

                var cotacao = await _cotacaoRepository.ObterUltimaFechamentoAsync(ticker);
                var precoVenda = cotacao?.PrecoFechamento ?? custodia.PrecoMedio;
                var qtd = custodia.Quantidade;
                var valorVenda = qtd * precoVenda;

                var lucro = custodia.RemoverAcoes(qtd, precoVenda);
                await _custodiaRepository.AtualizarAsync(custodia);

                totalVendas += valorVenda;
                lucroLiquido += lucro;

                vendas.Add(new OperacaoRebalanceamentoDto(ticker, qtd, precoVenda, valorVenda));

                await _rebalanceamentoRepository.AdicionarAsync(
                    Rebalanceamento.Criar(cliente.Id, TipoRebalanceamento.MudancaCesta,
                        ticker, qtd, precoVenda, null, 0, 0));
            }

            // RN-049: Rebalancear ativos que permaneceram mas mudaram de %
            var valorCarteira = 0m;
            foreach (var custodia in custodias.Where(c => c.Quantidade > 0))
            {
                var cot = await _cotacaoRepository.ObterUltimaFechamentoAsync(custodia.Ticker);
                valorCarteira += custodia.Quantidade * (cot?.PrecoFechamento ?? custodia.PrecoMedio);
            }
            valorCarteira += totalVendas; // Inclui valor obtido das vendas

            foreach (var ticker in tickersPermaneceram)
            {
                var custodia = custodias.FirstOrDefault(c => c.Ticker == ticker);
                if (custodia is null || custodia.Quantidade == 0) continue;

                var cotacao = await _cotacaoRepository.ObterUltimaFechamentoAsync(ticker);
                var precoAtual = cotacao?.PrecoFechamento ?? custodia.PrecoMedio;
                var valorAtual = custodia.Quantidade * precoAtual;

                var percentualAlvo = cestaNova.ObterPercentual(ticker);
                var valorAlvo = valorCarteira * (percentualAlvo / 100m);
                var qtdAlvo = (int)(valorAlvo / precoAtual);

                var diferenca = qtdAlvo - custodia.Quantidade;

                if (diferenca < 0) // Sobre-alocado: vender excesso
                {
                    var qtdVender = Math.Abs(diferenca);
                    var valorVendaExcesso = qtdVender * precoAtual;
                    var lucro = custodia.RemoverAcoes(qtdVender, precoAtual);
                    await _custodiaRepository.AtualizarAsync(custodia);

                    totalVendas += valorVendaExcesso;
                    lucroLiquido += lucro;

                    vendas.Add(new OperacaoRebalanceamentoDto(
                        ticker, qtdVender, precoAtual, valorVendaExcesso));

                    await _rebalanceamentoRepository.AdicionarAsync(
                        Rebalanceamento.Criar(cliente.Id, TipoRebalanceamento.MudancaCesta,
                            ticker, qtdVender, precoAtual, null, 0, 0));
                }
            }

            // RN-048: Comprar novos ativos com valor obtido das vendas
            if (totalVendas > 0 && tickersEntraram.Count > 0)
            {
                var somaPercentuaisNovos = tickersEntraram.Sum(t => cestaNova.ObterPercentual(t));

                foreach (var ticker in tickersEntraram)
                {
                    var percentual = cestaNova.ObterPercentual(ticker);
                    var proporcao = somaPercentuaisNovos > 0 ? percentual / somaPercentuaisNovos : 0;
                    var valorParaComprar = totalVendas * proporcao;

                    var cotacao = await _cotacaoRepository.ObterUltimaFechamentoAsync(ticker);
                    if (cotacao is null) continue;

                    var precoCompra = cotacao.PrecoFechamento;
                    var qtdComprar = (int)(valorParaComprar / precoCompra);
                    if (qtdComprar <= 0) continue;

                    // Criar ou atualizar custódia filhote
                    var custodiaFilhote = await _custodiaRepository
                        .ObterPorContaETickerAsync(contaFilhote.Id, ticker);
                    if (custodiaFilhote is null)
                    {
                        custodiaFilhote = Custodia.Criar(contaFilhote.Id, ticker);
                        await _custodiaRepository.AdicionarAsync(custodiaFilhote);
                        await _unitOfWork.CommitAsync();
                    }
                    custodiaFilhote.AdicionarAcoes(qtdComprar, precoCompra);

                    compras.Add(new OperacaoRebalanceamentoDto(
                        ticker, qtdComprar, precoCompra, qtdComprar * precoCompra));

                    await _rebalanceamentoRepository.AdicionarAsync(
                        Rebalanceamento.Criar(cliente.Id, TipoRebalanceamento.MudancaCesta,
                            "", 0, 0, ticker, qtdComprar, precoCompra));
                }
            }

            await _unitOfWork.CommitAsync();

            // RN-057 a RN-062: Verificar IR sobre vendas
            if (totalVendas > 0)
            {
                var totalVendasMes = await _rebalanceamentoRepository
                    .ObterTotalVendasClienteNoMesAsync(
                        cliente.Id, DateTime.UtcNow.Year, DateTime.UtcNow.Month);

                await _eventoIRService.RegistrarIRVendaAsync(
                    cliente.Id, cliente.CPF.Numero, totalVendasMes, lucroLiquido);
            }

            detalhes.Add(new RebalanceamentoClienteDto(
                cliente.Id, cliente.Nome, vendas, compras,
                totalVendas, compras.Sum(c => c.ValorTotal), lucroLiquido));
        }

        return new RebalanceamentoResponse(
            "MUDANCA_CESTA",
            detalhes.Count,
            detalhes,
            $"Rebalanceamento por mudança de cesta concluído. {detalhes.Count} clientes processados.");
    }

    public async Task<RebalanceamentoResponse> RebalancearPorDesvioAsync(decimal limiarPercentual = 5m)
    {
        var cestaAtiva = await _cestaRepository.ObterAtivaAsync()
            ?? throw new InvalidOperationException("Nenhuma cesta ativa encontrada.");

        var clientesAtivos = await _clienteRepository.ObterAtivosAsync();
        var detalhes = new List<RebalanceamentoClienteDto>();

        foreach (var cliente in clientesAtivos)
        {
            var contaFilhote = await _contaGraficaRepository.ObterPorClienteIdAsync(cliente.Id);
            if (contaFilhote is null) continue;

            var custodias = await _custodiaRepository.ObterPorContaGraficaIdAsync(contaFilhote.Id);
            if (custodias.Count == 0) continue;

            // Calcular valor total da carteira
            var valorCarteira = 0m;
            var cotacoes = new Dictionary<string, decimal>();

            foreach (var custodia in custodias.Where(c => c.Quantidade > 0))
            {
                var cotacao = await _cotacaoRepository.ObterUltimaFechamentoAsync(custodia.Ticker);
                var preco = cotacao?.PrecoFechamento ?? custodia.PrecoMedio;
                cotacoes[custodia.Ticker] = preco;
                valorCarteira += custodia.Quantidade * preco;
            }

            if (valorCarteira == 0) continue;

            // RN-050/051: Verificar desvio para cada ativo
            var temDesvio = false;
            foreach (var custodia in custodias.Where(c => c.Quantidade > 0))
            {
                var percentualReal = (custodia.Quantidade * cotacoes[custodia.Ticker]) / valorCarteira * 100m;
                var percentualAlvo = cestaAtiva.ObterPercentual(custodia.Ticker);
                var desvio = Math.Abs(percentualReal - percentualAlvo);

                if (desvio >= limiarPercentual)
                {
                    temDesvio = true;
                    break;
                }
            }

            if (!temDesvio) continue;

            // RN-052: Rebalancear vendendo sobre-alocados e comprando sub-alocados
            var vendas = new List<OperacaoRebalanceamentoDto>();
            var compras = new List<OperacaoRebalanceamentoDto>();
            decimal totalVendas = 0;
            decimal lucroLiquido = 0;

            foreach (var custodia in custodias.Where(c => c.Quantidade > 0))
            {
                var preco = cotacoes[custodia.Ticker];
                var percentualAlvo = cestaAtiva.ObterPercentual(custodia.Ticker);
                var valorAlvo = valorCarteira * (percentualAlvo / 100m);
                var qtdAlvo = (int)(valorAlvo / preco);

                var diferenca = qtdAlvo - custodia.Quantidade;

                if (diferenca < 0) // Sobre-alocado: vender
                {
                    var qtdVender = Math.Abs(diferenca);
                    var valorVenda = qtdVender * preco;
                    var lucro = custodia.RemoverAcoes(qtdVender, preco);
                    await _custodiaRepository.AtualizarAsync(custodia);

                    totalVendas += valorVenda;
                    lucroLiquido += lucro;

                    vendas.Add(new OperacaoRebalanceamentoDto(
                        custodia.Ticker, qtdVender, preco, valorVenda));

                    await _rebalanceamentoRepository.AdicionarAsync(
                        Rebalanceamento.Criar(cliente.Id, TipoRebalanceamento.DesvioProporcao,
                            custodia.Ticker, qtdVender, preco, null, 0, 0));
                }
                else if (diferenca > 0) // Sub-alocado: comprar
                {
                    custodia.AdicionarAcoes(diferenca, preco);

                    compras.Add(new OperacaoRebalanceamentoDto(
                        custodia.Ticker, diferenca, preco, diferenca * preco));

                    await _rebalanceamentoRepository.AdicionarAsync(
                        Rebalanceamento.Criar(cliente.Id, TipoRebalanceamento.DesvioProporcao,
                            "", 0, 0, custodia.Ticker, diferenca, preco));
                }
            }

            await _unitOfWork.CommitAsync();

            // RN-057 a RN-062: Verificar IR sobre vendas
            if (totalVendas > 0)
            {
                var totalVendasMes = await _rebalanceamentoRepository
                    .ObterTotalVendasClienteNoMesAsync(
                        cliente.Id, DateTime.UtcNow.Year, DateTime.UtcNow.Month);

                await _eventoIRService.RegistrarIRVendaAsync(
                    cliente.Id, cliente.CPF.Numero, totalVendasMes, lucroLiquido);
            }

            detalhes.Add(new RebalanceamentoClienteDto(
                cliente.Id, cliente.Nome, vendas, compras,
                totalVendas, compras.Sum(c => c.ValorTotal), lucroLiquido));
        }

        return new RebalanceamentoResponse(
            "DESVIO_PROPORCAO",
            detalhes.Count,
            detalhes,
            $"Rebalanceamento por desvio concluído. {detalhes.Count} clientes rebalanceados.");
    }
}
