using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Moq;

namespace CompraProgramada.UnitTests.Services;

public class ClienteServiceTests
{
    private readonly Mock<IClienteRepository> _clienteRepoMock = new();
    private readonly Mock<IContaGraficaRepository> _contaGraficaRepoMock = new();
    private readonly Mock<ICustodiaRepository> _custodiaRepoMock = new();
    private readonly Mock<ICotacaoRepository> _cotacaoRepoMock = new();
    private readonly Mock<IHistoricoValorMensalRepository> _historicoRepoMock = new();
    private readonly Mock<IDistribuicaoRepository> _distribuicaoRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ClienteService _service;

    public ClienteServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new ClienteService(
            _clienteRepoMock.Object,
            _contaGraficaRepoMock.Object,
            _custodiaRepoMock.Object,
            _cotacaoRepoMock.Object,
            _historicoRepoMock.Object,
            _distribuicaoRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    // ===== ADESÃO =====

    [Fact]
    public async Task AderirAsync_DadosValidos_RetornaAdesaoResponse()
    {
        var request = new AdesaoRequest("João da Silva", "52998224725", "joao@email.com", 3000m);

        _clienteRepoMock.Setup(r => r.ObterPorCpfAsync(It.IsAny<string>()))
            .ReturnsAsync((Cliente?)null);
        _contaGraficaRepoMock.Setup(r => r.ObterProximoNumeroContaAsync())
            .ReturnsAsync(1);

        var result = await _service.AderirAsync(request);

        Assert.NotNull(result);
        Assert.Equal("João da Silva", result.Nome);
        Assert.Equal("52998224725", result.Cpf);
        Assert.Equal(3000m, result.ValorMensal);
        Assert.True(result.Ativo);
        Assert.Equal("FLH-000001", result.ContaGrafica.NumeroConta);
        Assert.Equal("FILHOTE", result.ContaGrafica.Tipo);

        _clienteRepoMock.Verify(r => r.AdicionarAsync(It.IsAny<Cliente>()), Times.Once);
        _contaGraficaRepoMock.Verify(r => r.AdicionarAsync(It.IsAny<ContaGrafica>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AderirAsync_CpfDuplicado_LancaException()
    {
        var request = new AdesaoRequest("João", "52998224725", "joao@email.com", 3000m);
        var clienteExistente = Cliente.Criar("Existente", "52998224725", "exist@email.com", 500m);

        _clienteRepoMock.Setup(r => r.ObterPorCpfAsync(It.IsAny<string>()))
            .ReturnsAsync(clienteExistente);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AderirAsync(request));
        Assert.Contains("CLIENTE_CPF_DUPLICADO", ex.Message);
    }

    [Fact]
    public async Task AderirAsync_ValorAbaixoMinimo_LancaException()
    {
        var request = new AdesaoRequest("João", "52998224725", "joao@email.com", 50m);

        _clienteRepoMock.Setup(r => r.ObterPorCpfAsync(It.IsAny<string>()))
            .ReturnsAsync((Cliente?)null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AderirAsync(request));
    }

    // ===== SAÍDA =====

    [Fact]
    public async Task SairAsync_ClienteAtivo_RetornaSaidaResponse()
    {
        var cliente = Cliente.Criar("João", "52998224725", "joao@email.com", 1000m);

        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(1))
            .ReturnsAsync(cliente);

        var result = await _service.SairAsync(1);

        Assert.False(result.Ativo);
        Assert.NotNull(result.DataSaida);
        Assert.Contains("encerrada", result.Mensagem);
    }

    [Fact]
    public async Task SairAsync_ClienteNaoEncontrado_LancaKeyNotFoundException()
    {
        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(999))
            .ReturnsAsync((Cliente?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.SairAsync(999));
    }

    [Fact]
    public async Task SairAsync_ClienteJaInativo_LancaException()
    {
        var cliente = Cliente.Criar("João", "52998224725", "joao@email.com", 1000m);
        cliente.Sair(); // já inativo

        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(1))
            .ReturnsAsync(cliente);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SairAsync(1));
    }

    // ===== ALTERAR VALOR MENSAL =====

    [Fact]
    public async Task AlterarValorMensalAsync_ValorValido_RetornaResponse()
    {
        var cliente = Cliente.Criar("João", "52998224725", "joao@email.com", 1000m);
        var request = new AlterarValorMensalRequest(5000m);

        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(1))
            .ReturnsAsync(cliente);

        var result = await _service.AlterarValorMensalAsync(1, request);

        Assert.Equal(1000m, result.ValorMensalAnterior);
        Assert.Equal(5000m, result.ValorMensalNovo);
        Assert.Contains("atualizado", result.Mensagem);

        _historicoRepoMock.Verify(r =>
            r.AdicionarAsync(It.Is<HistoricoValorMensal>(h =>
                h.ValorAnterior == 1000m && h.ValorNovo == 5000m)), Times.Once);
    }

    [Fact]
    public async Task AlterarValorMensalAsync_ClienteInativo_LancaException()
    {
        var cliente = Cliente.Criar("João", "52998224725", "joao@email.com", 1000m);
        cliente.Sair();

        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(1))
            .ReturnsAsync(cliente);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AlterarValorMensalAsync(1, new AlterarValorMensalRequest(2000m)));
        Assert.Contains("CLIENTE_JA_INATIVO", ex.Message);
    }

    [Fact]
    public async Task AlterarValorMensalAsync_ValorAbaixoMinimo_LancaException()
    {
        var cliente = Cliente.Criar("João", "52998224725", "joao@email.com", 1000m);

        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(1))
            .ReturnsAsync(cliente);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AlterarValorMensalAsync(1, new AlterarValorMensalRequest(50m)));
    }

    // ===== CARTEIRA =====

    [Fact]
    public async Task ConsultarCarteiraAsync_ClienteComCustodia_RetornaCarteira()
    {
        var cliente = Cliente.Criar("João", "52998224725", "joao@email.com", 3000m);
        var contaGrafica = ContaGrafica.CriarFilhote(1, "FLH-000001");

        var custodia1 = Custodia.Criar(1, "PETR4");
        custodia1.AdicionarAcoes(10, 35.00m);

        var custodia2 = Custodia.Criar(1, "VALE3");
        custodia2.AdicionarAcoes(5, 60.00m);

        var cotacaoPetr = Cotacao.Criar(DateTime.Today, "PETR4", "02", 10, "PETROBRAS",
            37m, 37m, 37m, 37m, 37m, 1000, 37000m);
        var cotacaoVale = Cotacao.Criar(DateTime.Today, "VALE3", "02", 10, "VALE",
            65m, 65m, 65m, 65m, 65m, 1000, 65000m);

        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cliente);
        _contaGraficaRepoMock.Setup(r => r.ObterPorClienteIdAsync(1)).ReturnsAsync(contaGrafica);
        _custodiaRepoMock.Setup(r => r.ObterPorContaGraficaIdAsync(It.IsAny<long>()))
            .ReturnsAsync(new List<Custodia> { custodia1, custodia2 });
        _cotacaoRepoMock.Setup(r => r.ObterUltimaFechamentoAsync("PETR4")).ReturnsAsync(cotacaoPetr);
        _cotacaoRepoMock.Setup(r => r.ObterUltimaFechamentoAsync("VALE3")).ReturnsAsync(cotacaoVale);

        var result = await _service.ConsultarCarteiraAsync(1);

        Assert.Equal("João", result.Nome);
        Assert.Equal("FLH-000001", result.ContaGrafica);
        Assert.Equal(2, result.Ativos.Count);

        // PETR4: 10 x 35 = 350 investido, 10 x 37 = 370 atual, PL = 20
        var petr = result.Ativos.First(a => a.Ticker == "PETR4");
        Assert.Equal(10, petr.Quantidade);
        Assert.Equal(35m, petr.PrecoMedio);
        Assert.Equal(37m, petr.CotacaoAtual);
        Assert.Equal(370m, petr.ValorAtual);
        Assert.Equal(20m, petr.Pl);

        // VALE3: 5 x 60 = 300 investido, 5 x 65 = 325 atual, PL = 25
        var vale = result.Ativos.First(a => a.Ticker == "VALE3");
        Assert.Equal(5, vale.Quantidade);
        Assert.Equal(325m, vale.ValorAtual);
        Assert.Equal(25m, vale.Pl);

        // Resumo: investido 650, atual 695, PL 45
        Assert.Equal(650m, result.Resumo.ValorTotalInvestido);
        Assert.Equal(695m, result.Resumo.ValorAtualCarteira);
        Assert.Equal(45m, result.Resumo.PlTotal);
        Assert.Equal(6.92m, result.Resumo.RentabilidadePercentual);
    }

    [Fact]
    public async Task ConsultarCarteiraAsync_CarteiraSemPosicoes_RetornaVazio()
    {
        var cliente = Cliente.Criar("João", "52998224725", "joao@email.com", 3000m);
        var contaGrafica = ContaGrafica.CriarFilhote(1, "FLH-000001");

        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cliente);
        _contaGraficaRepoMock.Setup(r => r.ObterPorClienteIdAsync(1)).ReturnsAsync(contaGrafica);
        _custodiaRepoMock.Setup(r => r.ObterPorContaGraficaIdAsync(It.IsAny<long>()))
            .ReturnsAsync(new List<Custodia>());

        var result = await _service.ConsultarCarteiraAsync(1);

        Assert.Empty(result.Ativos);
        Assert.Equal(0m, result.Resumo.ValorTotalInvestido);
    }

    // ===== RENTABILIDADE =====

    [Fact]
    public async Task ConsultarRentabilidadeAsync_RetornaHistoricoEEvolucao()
    {
        var cliente = Cliente.Criar("João", "52998224725", "joao@email.com", 3000m);
        var contaGrafica = ContaGrafica.CriarFilhote(1, "FLH-000001");

        var custodia = Custodia.Criar(1, "PETR4");
        custodia.AdicionarAcoes(10, 35m);

        var cotacao = Cotacao.Criar(DateTime.Today, "PETR4", "02", 10, "PETROBRAS",
            37m, 37m, 37m, 37m, 37m, 1000, 37000m);

        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(1)).ReturnsAsync(cliente);
        _contaGraficaRepoMock.Setup(r => r.ObterPorClienteIdAsync(1)).ReturnsAsync(contaGrafica);
        _custodiaRepoMock.Setup(r => r.ObterPorContaGraficaIdAsync(It.IsAny<long>()))
            .ReturnsAsync(new List<Custodia> { custodia });
        _cotacaoRepoMock.Setup(r => r.ObterUltimaFechamentoAsync("PETR4")).ReturnsAsync(cotacao);
        _distribuicaoRepoMock.Setup(r => r.ObterPorClienteAsync(1))
            .ReturnsAsync(new List<Distribuicao>());

        var result = await _service.ConsultarRentabilidadeAsync(1);

        Assert.Equal("João", result.Nome);
        Assert.NotNull(result.Rentabilidade);
        Assert.Equal(350m, result.Rentabilidade.ValorTotalInvestido);
        Assert.Equal(370m, result.Rentabilidade.ValorAtualCarteira);
    }

    [Fact]
    public async Task ConsultarRentabilidadeAsync_ClienteNaoEncontrado_LancaException()
    {
        _clienteRepoMock.Setup(r => r.ObterPorIdAsync(999))
            .ReturnsAsync((Cliente?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.ConsultarRentabilidadeAsync(999));
    }
}
