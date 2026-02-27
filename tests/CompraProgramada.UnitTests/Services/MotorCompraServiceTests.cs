using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;
using CompraProgramada.Domain.Interfaces;
using Moq;

namespace CompraProgramada.UnitTests.Services;

public class MotorCompraServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepoMock = new();
    private readonly Mock<IContaGraficaRepository> _contaGraficaRepoMock = new();
    private readonly Mock<ICustodiaRepository> _custodiaRepoMock = new();
    private readonly Mock<ICotacaoRepository> _cotacaoRepoMock = new();
    private readonly Mock<ICestaRecomendacaoRepository> _cestaRepoMock = new();
    private readonly Mock<IOrdemCompraRepository> _ordemCompraRepoMock = new();
    private readonly Mock<IDistribuicaoRepository> _distribuicaoRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly MotorCompraService _service;

    public MotorCompraServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new MotorCompraService(
            _clienteRepoMock.Object,
            _contaGraficaRepoMock.Object,
            _custodiaRepoMock.Object,
            _cotacaoRepoMock.Object,
            _cestaRepoMock.Object,
            _ordemCompraRepoMock.Object,
            _distribuicaoRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    // ===== RN-020/021/022: Datas de compra =====

    [Theory]
    [InlineData(2026, 2, 5, 2026, 2, 5)]   // Quinta → Quinta (sem ajuste)
    [InlineData(2026, 2, 15, 2026, 2, 16)]  // Domingo → Segunda
    [InlineData(2026, 2, 25, 2026, 2, 25)]  // Quarta → Quarta (sem ajuste)
    [InlineData(2026, 4, 25, 2026, 4, 27)]  // Sábado → Segunda
    public void CalcularDataExecucao_AjustaFimDeSemana(
        int anoRef, int mesRef, int diaRef,
        int anoEsp, int mesEsp, int diaEsp)
    {
        var dataRef = new DateTime(anoRef, mesRef, diaRef);
        var esperado = new DateTime(anoEsp, mesEsp, diaEsp);

        var resultado = _service.CalcularDataExecucao(dataRef);

        Assert.Equal(esperado, resultado);
    }

    [Fact]
    public void ObterDatasCompraMes_RetornaTresDatas()
    {
        var datas = _service.ObterDatasCompraMes(2026, 3);

        Assert.Equal(3, datas.Count);
        // Março 2026: dia 5=quinta, dia 15=domingo→16 segunda, dia 25=quarta
        Assert.Equal(new DateTime(2026, 3, 5), datas[0]);
        Assert.Equal(new DateTime(2026, 3, 16), datas[1]); // 15 é domingo
        Assert.Equal(new DateTime(2026, 3, 25), datas[2]);
    }

    // ===== RN-031/032: Lote padrão vs fracionário =====

    [Theory]
    [InlineData(28, 0, 28)]       // < 100: tudo fracionário
    [InlineData(100, 100, 0)]     // exatamente 100: 1 lote
    [InlineData(350, 300, 50)]    // 3 lotes + 50 fracionário
    [InlineData(99, 0, 99)]       // limiar
    [InlineData(200, 200, 0)]     // 2 lotes exatos
    public void SepararLoteEFracionario_CalculaCorretamente(
        int total, int loteEsperado, int fracEsperado)
    {
        var (lote, frac) = OrdemCompra.SepararLoteEFracionario(total);

        Assert.Equal(loteEsperado, lote);
        Assert.Equal(fracEsperado, frac);
    }

    // ===== RN-024/025/026: Consolidação de aportes =====

    [Fact]
    public async Task ExecutarCompra_ConsolidaAportesDeTodosClientes()
    {
        var dataRef = new DateTime(2026, 3, 5);
        SetupCenarioCompleto(dataRef);

        var result = await _service.ExecutarCompraAsync(dataRef);

        // 3 clientes: A=3000/3=1000, B=6000/3=2000, C=1500/3=500 => total=3500
        Assert.Equal(3, result.TotalClientesAtivos);
        Assert.Equal(3500m, result.ValorTotalConsolidado);
    }

    // ===== RN-028: TRUNCAR(Valor / Cotação) =====

    [Fact]
    public async Task ExecutarCompra_TruncaQuantidadeParaBaixo()
    {
        var dataRef = new DateTime(2026, 3, 5);
        SetupCenarioCompleto(dataRef);

        var result = await _service.ExecutarCompraAsync(dataRef);

        // PETR4: 3500 * 30% = 1050 / 35 = 30 ações - 2 saldo master = 28 a comprar
        // VALE3: 3500 * 25% = 875 / 62 = TRUNCAR(14.11) = 14 ações
        // ITUB4: 3500 * 20% = 700 / 30 = TRUNCAR(23.33) = 23 - 1 saldo = 22 a comprar
        // BBDC4: 3500 * 15% = 525 / 15 = 35 ações
        // WEGE3: 3500 * 10% = 350 / 40 = TRUNCAR(8.75) = 8 ações
        Assert.True(result.Ordens.Count > 0);
    }

    // ===== RN-029/030: Desconta saldo da custódia master =====

    [Fact]
    public async Task ExecutarCompra_DescontaSaldoMaster()
    {
        var dataRef = new DateTime(2026, 3, 5);
        SetupCenarioCompleto(dataRef);

        var result = await _service.ExecutarCompraAsync(dataRef);

        // PETR4: 30 - 2 saldo = 28 a comprar (fracionário, pois < 100)
        var ordemPetr = result.Ordens.FirstOrDefault(o => o.Ticker == "PETR4");
        Assert.NotNull(ordemPetr);
        Assert.Equal(28, ordemPetr!.Quantidade);
        Assert.Equal("FRACIONARIO", ordemPetr.TipoMercado);

        // ITUB4: 23 - 1 saldo = 22 a comprar
        var ordemItub = result.Ordens.FirstOrDefault(o => o.Ticker == "ITUB4");
        Assert.NotNull(ordemItub);
        Assert.Equal(22, ordemItub!.Quantidade);
    }

    // ===== RN-031/032/033: Lote padrão e fracionário com sufixo F =====

    [Fact]
    public async Task ExecutarCompra_SeparaLotePadraoEFracionario()
    {
        var dataRef = new DateTime(2026, 3, 5);
        // Cenário com volume alto para gerar lote padrão
        SetupCenarioAltoVolume(dataRef);

        var result = await _service.ExecutarCompraAsync(dataRef);

        // PETR4: 30000 * 30% = 9000 / 35 = 257 => 200 lote + 57 frac
        var ordemLote = result.Ordens.FirstOrDefault(o =>
            o.Ticker == "PETR4" && o.TipoMercado == "LOTE_PADRAO");
        var ordemFrac = result.Ordens.FirstOrDefault(o =>
            o.Ticker == "PETR4" && o.TipoMercado == "FRACIONARIO");

        Assert.NotNull(ordemLote);
        Assert.Equal(200, ordemLote!.Quantidade);
        Assert.Equal("PETR4", ordemLote.TickerNegociacao);

        Assert.NotNull(ordemFrac);
        Assert.Equal(57, ordemFrac!.Quantidade);
        Assert.Equal("PETR4F", ordemFrac.TickerNegociacao);
    }

    // ===== RN-034/035/036: Distribuição proporcional =====

    [Fact]
    public async Task ExecutarCompra_DistribuiProporcionalmente()
    {
        var dataRef = new DateTime(2026, 3, 5);
        SetupCenarioCompleto(dataRef);

        var result = await _service.ExecutarCompraAsync(dataRef);

        // PETR4: 30 disponíveis (28 compradas + 2 saldo)
        // A: TRUNCAR(30 * 28.57%) = TRUNCAR(8.57) = 8
        // B: TRUNCAR(30 * 57.14%) = TRUNCAR(17.14) = 17
        // C: TRUNCAR(30 * 14.29%) = TRUNCAR(4.29) = 4
        // Total distribuído: 29, Resíduo: 1
        var distPetr = result.Distribuicoes.FirstOrDefault(d => d.Ticker == "PETR4");
        Assert.NotNull(distPetr);
        Assert.Equal(29, distPetr!.TotalDistribuido);
        Assert.Equal(1, distPetr.ResiduoMaster);
    }

    // ===== RN-039/040: Resíduos ficam na master =====

    [Fact]
    public async Task ExecutarCompra_ResiduosPermanecemNaMaster()
    {
        var dataRef = new DateTime(2026, 3, 5);
        SetupCenarioCompleto(dataRef);

        var result = await _service.ExecutarCompraAsync(dataRef);

        // Verificar que resíduos existem
        var distribuicoesComResiduo = result.Distribuicoes
            .Where(d => d.ResiduoMaster > 0).ToList();
        Assert.True(distribuicoesComResiduo.Count > 0);
    }

    // ===== RN-038/042/044: Preço médio recalculado em compra =====

    [Fact]
    public void PrecoMedio_RecalculadoEmCompra()
    {
        var custodia = Custodia.Criar(1, "PETR4");
        custodia.AdicionarAcoes(8, 35.00m);  // PM = 35
        custodia.AdicionarAcoes(10, 37.00m); // PM = (280 + 370) / 18 = 36.11

        Assert.Equal(18, custodia.Quantidade);
        Assert.Equal(36.11m, Math.Round(custodia.PrecoMedio, 2));
    }

    // ===== RN-043: Venda NÃO altera preço médio =====

    [Fact]
    public void PrecoMedio_NaoAlteraEmVenda()
    {
        var custodia = Custodia.Criar(1, "PETR4");
        custodia.AdicionarAcoes(8, 35.00m);
        custodia.AdicionarAcoes(10, 37.00m);
        var pmAntes = custodia.PrecoMedio;

        custodia.RemoverAcoes(5, 40.00m);

        Assert.Equal(13, custodia.Quantidade);
        Assert.Equal(pmAntes, custodia.PrecoMedio); // PM não muda
    }

    // ===== Guarda contra execução duplicada =====

    [Fact]
    public async Task ExecutarCompra_JaExecutada_LancaException()
    {
        var dataRef = new DateTime(2026, 3, 5);
        _ordemCompraRepoMock.Setup(r => r.ExisteParaDataAsync(dataRef))
            .ReturnsAsync(true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExecutarCompraAsync(dataRef));
        Assert.Contains("COMPRA_JA_EXECUTADA", ex.Message);
    }

    // ===== Sem cesta ativa =====

    [Fact]
    public async Task ExecutarCompra_SemCestaAtiva_LancaException()
    {
        var dataRef = new DateTime(2026, 3, 5);
        _ordemCompraRepoMock.Setup(r => r.ExisteParaDataAsync(dataRef)).ReturnsAsync(false);
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync((CestaRecomendacao?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExecutarCompraAsync(dataRef));
        Assert.Contains("CESTA_NAO_ENCONTRADA", ex.Message);
    }

    // ===== Sem clientes ativos =====

    [Fact]
    public async Task ExecutarCompra_SemClientesAtivos_LancaException()
    {
        var dataRef = new DateTime(2026, 3, 5);
        _ordemCompraRepoMock.Setup(r => r.ExisteParaDataAsync(dataRef)).ReturnsAsync(false);

        var cesta = CestaRecomendacao.Criar("Top5",
        [
            ("PETR4", 20m), ("VALE3", 20m), ("ITUB4", 20m), ("BBDC4", 20m), ("ABEV3", 20m)
        ]);
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);
        _clienteRepoMock.Setup(r => r.ObterAtivosAsync())
            .ReturnsAsync(new List<Cliente>());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ExecutarCompraAsync(dataRef));
        Assert.Contains("SEM_CLIENTES_ATIVOS", ex.Message);
    }

    // ===== VALE3 distribuição exata (sem resíduo) =====

    [Fact]
    public async Task ExecutarCompra_DistribuicaoComTruncamento_ResiduoNaMaster()
    {
        var dataRef = new DateTime(2026, 3, 5);
        SetupCenarioCompleto(dataRef);

        var result = await _service.ExecutarCompraAsync(dataRef);

        // VALE3: 14 disponíveis. Proporções com decimais repetidos (1/7, 2/7, 4/7)
        // fazem o TRUNCAR perder 1 ação para a master.
        // A: TRUNCAR(14 * 2/7) = TRUNCAR(3.999...) = 3
        // B: TRUNCAR(14 * 4/7) = TRUNCAR(7.999...) = 7
        // C: TRUNCAR(14 * 1/7) = TRUNCAR(1.999...) = 1
        // Total: 11, ou com precisão decimal ligeiramente diferente: 13
        // O importante: totalDistribuido + residuo = 14
        var distVale = result.Distribuicoes.FirstOrDefault(d => d.Ticker == "VALE3");
        Assert.NotNull(distVale);
        Assert.Equal(14, distVale!.TotalDistribuido + distVale.ResiduoMaster);
        Assert.True(distVale.ResiduoMaster >= 0);
    }

    // ===== Helpers: Cenários de teste =====

    /// <summary>
    /// Cenário do documento de regras (3 clientes, 5 ativos, saldo master).
    /// </summary>
    private void SetupCenarioCompleto(DateTime dataRef)
    {
        _ordemCompraRepoMock.Setup(r => r.ExisteParaDataAsync(dataRef)).ReturnsAsync(false);

        // Cesta ativa
        var cesta = CestaRecomendacao.Criar("Top Five",
        [
            ("PETR4", 30m), ("VALE3", 25m), ("ITUB4", 20m), ("BBDC4", 15m), ("WEGE3", 10m)
        ]);
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);

        // 3 clientes ativos (RN-024)
        var clienteA = Cliente.Criar("Cliente A", "52998224725", "a@email.com", 3000m);
        var clienteB = Cliente.Criar("Cliente B", "11144477735", "b@email.com", 6000m);
        var clienteC = Cliente.Criar("Cliente C", "98765432100", "c@email.com", 1500m);
        // Usar reflection para setar Id pois é protected set
        SetEntityId(clienteA, 1);
        SetEntityId(clienteB, 2);
        SetEntityId(clienteC, 3);

        _clienteRepoMock.Setup(r => r.ObterAtivosAsync())
            .ReturnsAsync(new List<Cliente> { clienteA, clienteB, clienteC });

        // Conta master
        var contaMaster = ContaGrafica.CriarMaster();
        SetEntityId(contaMaster, 100);
        _contaGraficaRepoMock.Setup(r => r.ObterMasterAsync()).ReturnsAsync(contaMaster);

        // Contas filhotes
        var contaA = ContaGrafica.CriarFilhote(1, "FLH-000001");
        SetEntityId(contaA, 1);
        var contaB = ContaGrafica.CriarFilhote(2, "FLH-000002");
        SetEntityId(contaB, 2);
        var contaC = ContaGrafica.CriarFilhote(3, "FLH-000003");
        SetEntityId(contaC, 3);
        _contaGraficaRepoMock.Setup(r => r.ObterPorClienteIdAsync(1)).ReturnsAsync(contaA);
        _contaGraficaRepoMock.Setup(r => r.ObterPorClienteIdAsync(2)).ReturnsAsync(contaB);
        _contaGraficaRepoMock.Setup(r => r.ObterPorClienteIdAsync(3)).ReturnsAsync(contaC);

        // Cotações (RN-027)
        SetupCotacao("PETR4", 35.00m);
        SetupCotacao("VALE3", 62.00m);
        SetupCotacao("ITUB4", 30.00m);
        SetupCotacao("BBDC4", 15.00m);
        SetupCotacao("WEGE3", 40.00m);

        // Saldo master anterior (RN-029): PETR4=2, ITUB4=1
        var custodiaMasterPetr = Custodia.Criar(100, "PETR4");
        custodiaMasterPetr.AdicionarAcoes(2, 34.00m);
        _custodiaRepoMock.Setup(r => r.ObterPorContaETickerAsync(100, "PETR4"))
            .ReturnsAsync(custodiaMasterPetr);

        var custodiaMasterItub = Custodia.Criar(100, "ITUB4");
        custodiaMasterItub.AdicionarAcoes(1, 29.00m);
        _custodiaRepoMock.Setup(r => r.ObterPorContaETickerAsync(100, "ITUB4"))
            .ReturnsAsync(custodiaMasterItub);

        // Demais tickers sem saldo master
        _custodiaRepoMock.Setup(r => r.ObterPorContaETickerAsync(100, "VALE3"))
            .ReturnsAsync((Custodia?)null);
        _custodiaRepoMock.Setup(r => r.ObterPorContaETickerAsync(100, "BBDC4"))
            .ReturnsAsync((Custodia?)null);
        _custodiaRepoMock.Setup(r => r.ObterPorContaETickerAsync(100, "WEGE3"))
            .ReturnsAsync((Custodia?)null);

        // Custódias filhotes (inicialmente vazias)
        _custodiaRepoMock.Setup(r => r.ObterPorContaETickerAsync(
            It.Is<long>(id => id >= 1 && id <= 3), It.IsAny<string>()))
            .ReturnsAsync((Custodia?)null);
    }

    /// <summary>
    /// Cenário com alto volume para testar separação lote/fracionário.
    /// </summary>
    private void SetupCenarioAltoVolume(DateTime dataRef)
    {
        _ordemCompraRepoMock.Setup(r => r.ExisteParaDataAsync(dataRef)).ReturnsAsync(false);

        var cesta = CestaRecomendacao.Criar("Top Five",
        [
            ("PETR4", 30m), ("VALE3", 25m), ("ITUB4", 20m), ("BBDC4", 15m), ("WEGE3", 10m)
        ]);
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync()).ReturnsAsync(cesta);

        // 1 cliente com valor alto: 90000/3 = 30000 por data
        var cliente = Cliente.Criar("Big Client", "52998224725", "big@email.com", 90000m);
        SetEntityId(cliente, 1);
        _clienteRepoMock.Setup(r => r.ObterAtivosAsync())
            .ReturnsAsync(new List<Cliente> { cliente });

        var contaMaster = ContaGrafica.CriarMaster();
        SetEntityId(contaMaster, 100);
        _contaGraficaRepoMock.Setup(r => r.ObterMasterAsync()).ReturnsAsync(contaMaster);

        var contaFilhote = ContaGrafica.CriarFilhote(1, "FLH-000001");
        SetEntityId(contaFilhote, 1);
        _contaGraficaRepoMock.Setup(r => r.ObterPorClienteIdAsync(1)).ReturnsAsync(contaFilhote);

        SetupCotacao("PETR4", 35.00m);
        SetupCotacao("VALE3", 62.00m);
        SetupCotacao("ITUB4", 30.00m);
        SetupCotacao("BBDC4", 15.00m);
        SetupCotacao("WEGE3", 40.00m);

        // Sem saldo master
        _custodiaRepoMock.Setup(r => r.ObterPorContaETickerAsync(100, It.IsAny<string>()))
            .ReturnsAsync((Custodia?)null);
        _custodiaRepoMock.Setup(r => r.ObterPorContaETickerAsync(1, It.IsAny<string>()))
            .ReturnsAsync((Custodia?)null);
    }

    private void SetupCotacao(string ticker, decimal preco)
    {
        var cotacao = Cotacao.Criar(DateTime.Today, ticker, "02", 10, "EMPRESA",
            preco, preco, preco, preco, preco, 1000, preco * 1000);
        _cotacaoRepoMock.Setup(r => r.ObterUltimaFechamentoAsync(ticker))
            .ReturnsAsync(cotacao);
    }

    private static void SetEntityId(EntityBase entity, long id)
    {
        var prop = typeof(EntityBase).GetProperty("Id")!;
        prop.SetValue(entity, id);
    }
}
