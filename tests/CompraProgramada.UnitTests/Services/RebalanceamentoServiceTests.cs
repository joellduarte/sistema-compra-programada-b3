using CompraProgramada.Application.Interfaces;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Moq;

namespace CompraProgramada.UnitTests.Services;

public class RebalanceamentoServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepoMock = new();
    private readonly Mock<IContaGraficaRepository> _contaGraficaRepoMock = new();
    private readonly Mock<ICustodiaRepository> _custodiaRepoMock = new();
    private readonly Mock<ICotacaoRepository> _cotacaoRepoMock = new();
    private readonly Mock<ICestaRecomendacaoRepository> _cestaRepoMock = new();
    private readonly Mock<IRebalanceamentoRepository> _rebalRepoMock = new();
    private readonly Mock<IEventoIRService> _eventoIRServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly RebalanceamentoService _service;

    public RebalanceamentoServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new RebalanceamentoService(
            _clienteRepoMock.Object,
            _contaGraficaRepoMock.Object,
            _custodiaRepoMock.Object,
            _cotacaoRepoMock.Object,
            _cestaRepoMock.Object,
            _rebalRepoMock.Object,
            _eventoIRServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    // ===== RN-046/047: Vende ativos que saíram da cesta =====

    [Fact]
    public async Task MudancaCesta_VendeAtivoQuesSairam()
    {
        SetupCenarioMudancaCesta();

        var result = await _service.RebalancearPorMudancaCestaAsync(1, 2);

        Assert.Equal(1, result.TotalClientesRebalanceados);
        var detalhe = result.Detalhes[0];

        // BBDC4 e WEGE3 saíram → devem ser vendidos
        var vendaBBDC = detalhe.Vendas.FirstOrDefault(v => v.Ticker == "BBDC4");
        var vendaWEGE = detalhe.Vendas.FirstOrDefault(v => v.Ticker == "WEGE3");
        Assert.NotNull(vendaBBDC);
        Assert.NotNull(vendaWEGE);
        Assert.Equal(10, vendaBBDC!.Quantidade);
        Assert.Equal(2, vendaWEGE!.Quantidade);
    }

    // ===== RN-048: Compra novos ativos com valor da venda =====

    [Fact]
    public async Task MudancaCesta_CompraNovoAtivosComValorDaVenda()
    {
        SetupCenarioMudancaCesta();

        var result = await _service.RebalancearPorMudancaCestaAsync(1, 2);

        var detalhe = result.Detalhes[0];

        // ABEV3 e RENT3 entraram → devem ser comprados
        Assert.True(detalhe.Compras.Count > 0);
        var compraABEV = detalhe.Compras.FirstOrDefault(c => c.Ticker == "ABEV3");
        var compraRENT = detalhe.Compras.FirstOrDefault(c => c.Ticker == "RENT3");
        Assert.NotNull(compraABEV);
        Assert.NotNull(compraRENT);
    }

    // ===== RN-049: Rebalanceia ativos com percentual alterado =====

    [Fact]
    public async Task MudancaCesta_VendeExcessoDeAtivoQuePercentualDiminuiu()
    {
        SetupCenarioMudancaCesta();

        var result = await _service.RebalancearPorMudancaCestaAsync(1, 2);

        var detalhe = result.Detalhes[0];
        // PETR4 foi de 30% para 25% → deve vender excesso
        var vendaPETR = detalhe.Vendas.FirstOrDefault(v => v.Ticker == "PETR4");
        Assert.NotNull(vendaPETR);
        Assert.True(vendaPETR!.Quantidade > 0);
    }

    // ===== Integração com IR (RN-057 a RN-062) =====

    [Fact]
    public async Task MudancaCesta_ChamaEventoIRVendaAposVendas()
    {
        SetupCenarioMudancaCesta();
        _rebalRepoMock.Setup(r => r.ObterTotalVendasClienteNoMesAsync(
                It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(230m); // Total vendas no mês

        var result = await _service.RebalancearPorMudancaCestaAsync(1, 2);

        // Deve chamar RegistrarIRVendaAsync com o total de vendas
        _eventoIRServiceMock.Verify(s => s.RegistrarIRVendaAsync(
            It.IsAny<long>(), It.IsAny<string>(),
            It.IsAny<decimal>(), It.IsAny<decimal>()), Times.Once);
    }

    // ===== RN-050/051: Desvio de proporção detectado =====

    [Fact]
    public async Task Desvio_RebalanceiaQuandoDesvioExcedeLimiar()
    {
        SetupCenarioDesvio();

        var result = await _service.RebalancearPorDesvioAsync(5m);

        Assert.Equal(1, result.TotalClientesRebalanceados);
        Assert.Equal("DESVIO_PROPORCAO", result.Tipo);
    }

    // ===== RN-050: Sem desvio significativo, não rebalanceia =====

    [Fact]
    public async Task Desvio_NaoRebalanceiaSemDesvioSignificativo()
    {
        SetupCenarioSemDesvio();

        var result = await _service.RebalancearPorDesvioAsync(5m);

        Assert.Equal(0, result.TotalClientesRebalanceados);
    }

    // ===== RN-052: Vende sobre-alocados =====

    [Fact]
    public async Task Desvio_VendeSobreAlocados()
    {
        SetupCenarioDesvio();

        var result = await _service.RebalancearPorDesvioAsync(5m);

        var detalhe = result.Detalhes[0];
        Assert.True(detalhe.Vendas.Count > 0);
    }

    // ===== Sem cesta ativa → erro =====

    [Fact]
    public async Task Desvio_SemCestaAtiva_LancaException()
    {
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync())
            .ReturnsAsync((CestaRecomendacao?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RebalancearPorDesvioAsync());
    }

    // ===== Registra rebalanceamento no repositório =====

    [Fact]
    public async Task MudancaCesta_RegistraRebalanceamentoNoBanco()
    {
        SetupCenarioMudancaCesta();

        await _service.RebalancearPorMudancaCestaAsync(1, 2);

        _rebalRepoMock.Verify(r => r.AdicionarAsync(
            It.IsAny<Rebalanceamento>()), Times.AtLeastOnce);
    }

    // ===== Helpers =====

    private void SetupCenarioMudancaCesta()
    {
        // Cesta anterior: PETR4(30%), VALE3(25%), ITUB4(20%), BBDC4(15%), WEGE3(10%)
        var cestaAnterior = CestaRecomendacao.Criar("Anterior",
        [
            ("PETR4", 30m), ("VALE3", 25m), ("ITUB4", 20m), ("BBDC4", 15m), ("WEGE3", 10m)
        ]);
        SetEntityId(cestaAnterior, 1);

        // Nova cesta: PETR4(25%), VALE3(20%), ITUB4(20%), ABEV3(20%), RENT3(15%)
        var cestaNova = CestaRecomendacao.Criar("Nova",
        [
            ("PETR4", 25m), ("VALE3", 20m), ("ITUB4", 20m), ("ABEV3", 20m), ("RENT3", 15m)
        ]);
        SetEntityId(cestaNova, 2);

        _cestaRepoMock.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cestaAnterior);
        _cestaRepoMock.Setup(r => r.ObterPorIdAsync(2)).ReturnsAsync(cestaNova);

        // 1 cliente ativo
        var cliente = Cliente.Criar("Cliente A", "52998224725", "a@email.com", 3000m);
        SetEntityId(cliente, 1);
        _clienteRepoMock.Setup(r => r.ObterAtivosAsync())
            .ReturnsAsync(new List<Cliente> { cliente });

        // Conta filhote
        var conta = ContaGrafica.CriarFilhote(1, "FLH-000001");
        SetEntityId(conta, 1);
        _contaGraficaRepoMock.Setup(r => r.ObterPorClienteIdAsync(1)).ReturnsAsync(conta);

        // Custódias atuais (conforme exemplo do documento)
        var custPETR = Custodia.Criar(1, "PETR4"); custPETR.AdicionarAcoes(8, 35m);
        var custVALE = Custodia.Criar(1, "VALE3"); custVALE.AdicionarAcoes(4, 62m);
        var custITUB = Custodia.Criar(1, "ITUB4"); custITUB.AdicionarAcoes(6, 30m);
        var custBBDC = Custodia.Criar(1, "BBDC4"); custBBDC.AdicionarAcoes(10, 15m);
        var custWEGE = Custodia.Criar(1, "WEGE3"); custWEGE.AdicionarAcoes(2, 40m);
        SetEntityId(custPETR, 1); SetEntityId(custVALE, 2); SetEntityId(custITUB, 3);
        SetEntityId(custBBDC, 4); SetEntityId(custWEGE, 5);

        _custodiaRepoMock.Setup(r => r.ObterPorContaGraficaIdAsync(1))
            .ReturnsAsync(new List<Custodia> { custPETR, custVALE, custITUB, custBBDC, custWEGE });

        // Cotações atuais
        SetupCotacao("PETR4", 35m); SetupCotacao("VALE3", 62m); SetupCotacao("ITUB4", 30m);
        SetupCotacao("BBDC4", 15m); SetupCotacao("WEGE3", 40m);
        SetupCotacao("ABEV3", 14m); SetupCotacao("RENT3", 48m);

        // Custódias para novos ativos (não existem ainda)
        _custodiaRepoMock.Setup(r => r.ObterPorContaETickerAsync(1, "ABEV3"))
            .ReturnsAsync((Custodia?)null);
        _custodiaRepoMock.Setup(r => r.ObterPorContaETickerAsync(1, "RENT3"))
            .ReturnsAsync((Custodia?)null);

        _rebalRepoMock.Setup(r => r.ObterTotalVendasClienteNoMesAsync(
                It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);
    }

    private void SetupCenarioDesvio()
    {
        // Cesta ativa: 20% cada
        var cesta = CestaRecomendacao.Criar("Cesta",
        [
            ("PETR4", 20m), ("VALE3", 20m), ("ITUB4", 20m), ("BBDC4", 20m), ("ABEV3", 20m)
        ]);
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);

        var cliente = Cliente.Criar("Cliente A", "52998224725", "a@email.com", 3000m);
        SetEntityId(cliente, 1);
        _clienteRepoMock.Setup(r => r.ObterAtivosAsync())
            .ReturnsAsync(new List<Cliente> { cliente });

        var conta = ContaGrafica.CriarFilhote(1, "FLH-000001");
        SetEntityId(conta, 1);
        _contaGraficaRepoMock.Setup(r => r.ObterPorClienteIdAsync(1)).ReturnsAsync(conta);

        // PETR4 sobre-alocado (35% vs 20% alvo = 15pp desvio > 5pp limiar)
        var custPETR = Custodia.Criar(1, "PETR4"); custPETR.AdicionarAcoes(35, 35m); // R$1225 = 35%
        var custVALE = Custodia.Criar(1, "VALE3"); custVALE.AdicionarAcoes(8, 62m);  // R$496 = ~14%
        var custITUB = Custodia.Criar(1, "ITUB4"); custITUB.AdicionarAcoes(17, 30m); // R$510 = ~15%
        var custBBDC = Custodia.Criar(1, "BBDC4"); custBBDC.AdicionarAcoes(20, 15m); // R$300 = ~9%
        var custABEV = Custodia.Criar(1, "ABEV3"); custABEV.AdicionarAcoes(67, 14m); // R$938 = ~27%

        _custodiaRepoMock.Setup(r => r.ObterPorContaGraficaIdAsync(1))
            .ReturnsAsync(new List<Custodia> { custPETR, custVALE, custITUB, custBBDC, custABEV });

        SetupCotacao("PETR4", 35m); SetupCotacao("VALE3", 62m); SetupCotacao("ITUB4", 30m);
        SetupCotacao("BBDC4", 15m); SetupCotacao("ABEV3", 14m);

        _rebalRepoMock.Setup(r => r.ObterTotalVendasClienteNoMesAsync(
                It.IsAny<long>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(0m);
    }

    private void SetupCenarioSemDesvio()
    {
        var cesta = CestaRecomendacao.Criar("Cesta",
        [
            ("PETR4", 20m), ("VALE3", 20m), ("ITUB4", 20m), ("BBDC4", 20m), ("ABEV3", 20m)
        ]);
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);

        var cliente = Cliente.Criar("Cliente A", "52998224725", "a@email.com", 3000m);
        SetEntityId(cliente, 1);
        _clienteRepoMock.Setup(r => r.ObterAtivosAsync())
            .ReturnsAsync(new List<Cliente> { cliente });

        var conta = ContaGrafica.CriarFilhote(1, "FLH-000001");
        SetEntityId(conta, 1);
        _contaGraficaRepoMock.Setup(r => r.ObterPorClienteIdAsync(1)).ReturnsAsync(conta);

        // Todos próximos de 20% (desvio < 5pp)
        var custPETR = Custodia.Criar(1, "PETR4"); custPETR.AdicionarAcoes(6, 35m);  // R$210 = 21%
        var custVALE = Custodia.Criar(1, "VALE3"); custVALE.AdicionarAcoes(3, 62m);  // R$186 = 19%
        var custITUB = Custodia.Criar(1, "ITUB4"); custITUB.AdicionarAcoes(7, 30m);  // R$210 = 21%
        var custBBDC = Custodia.Criar(1, "BBDC4"); custBBDC.AdicionarAcoes(13, 15m); // R$195 = 20%
        var custABEV = Custodia.Criar(1, "ABEV3"); custABEV.AdicionarAcoes(14, 14m); // R$196 = 20%

        _custodiaRepoMock.Setup(r => r.ObterPorContaGraficaIdAsync(1))
            .ReturnsAsync(new List<Custodia> { custPETR, custVALE, custITUB, custBBDC, custABEV });

        SetupCotacao("PETR4", 35m); SetupCotacao("VALE3", 62m); SetupCotacao("ITUB4", 30m);
        SetupCotacao("BBDC4", 15m); SetupCotacao("ABEV3", 14m);
    }

    private void SetupCotacao(string ticker, decimal preco)
    {
        var cotacao = Cotacao.Criar(DateTime.Today, ticker, "02", 10, "EMPRESA",
            preco, preco, preco, preco, preco, 1000, preco * 1000);
        _cotacaoRepoMock.Setup(r => r.ObterUltimaFechamentoAsync(ticker)).ReturnsAsync(cotacao);
    }

    private static void SetEntityId(EntityBase entity, long id)
    {
        typeof(EntityBase).GetProperty("Id")!.SetValue(entity, id);
    }
}
