using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Moq;

namespace CompraProgramada.UnitTests.Services;

public class CestaServiceTests
{
    private readonly Mock<ICestaRecomendacaoRepository> _cestaRepoMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly CestaService _service;

    public CestaServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new CestaService(
            _cestaRepoMock.Object,
            _unitOfWorkMock.Object);
    }

    private static CriarCestaRequest CriarRequestValido() =>
        new("Top Five Fev/2026",
        [
            new("PETR4", 30m),
            new("VALE3", 25m),
            new("ITUB4", 20m),
            new("BBDC4", 15m),
            new("ABEV3", 10m)
        ]);

    // ===== CRIAR CESTA =====

    [Fact]
    public async Task CriarCestaAsync_DadosValidos_RetornaCestaAtiva()
    {
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync())
            .ReturnsAsync((CestaRecomendacao?)null);

        var result = await _service.CriarCestaAsync(CriarRequestValido());

        Assert.NotNull(result);
        Assert.Equal("Top Five Fev/2026", result.Nome);
        Assert.True(result.Ativa);
        Assert.Equal(5, result.Itens.Count);
        Assert.Equal(100m, result.Itens.Sum(i => i.Percentual));

        _cestaRepoMock.Verify(r => r.AdicionarAsync(It.IsAny<CestaRecomendacao>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===== RN-017/018: Desativa cesta anterior na mesma transação =====

    [Fact]
    public async Task CriarCestaAsync_CestaAnteriorExiste_DesativaAnterior()
    {
        var cestaAnterior = CestaRecomendacao.Criar("Antiga",
        [
            ("PETR4", 20m), ("VALE3", 20m), ("ITUB4", 20m), ("BBDC4", 20m), ("ABEV3", 20m)
        ]);

        _cestaRepoMock.Setup(r => r.ObterAtivaAsync())
            .ReturnsAsync(cestaAnterior);

        var result = await _service.CriarCestaAsync(CriarRequestValido());

        Assert.True(result.Ativa); // nova está ativa
        Assert.False(cestaAnterior.Ativa); // anterior desativada
        Assert.NotNull(cestaAnterior.DataDesativacao);

        _cestaRepoMock.Verify(r => r.AtualizarAsync(cestaAnterior), Times.Once);
        _cestaRepoMock.Verify(r => r.AdicionarAsync(It.IsAny<CestaRecomendacao>()), Times.Once);
        // Tudo em uma única transação (1 commit)
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===== RN-014: Exatamente 5 ativos =====

    [Fact]
    public async Task CriarCestaAsync_MenosDe5Ativos_LancaException()
    {
        var request = new CriarCestaRequest("Cesta",
        [
            new("PETR4", 50m),
            new("VALE3", 50m)
        ]);

        _cestaRepoMock.Setup(r => r.ObterAtivaAsync())
            .ReturnsAsync((CestaRecomendacao?)null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CriarCestaAsync(request));
    }

    // ===== RN-015: Soma = 100% =====

    [Fact]
    public async Task CriarCestaAsync_SomaNao100_LancaException()
    {
        var request = new CriarCestaRequest("Cesta",
        [
            new("PETR4", 30m),
            new("VALE3", 25m),
            new("ITUB4", 20m),
            new("BBDC4", 15m),
            new("ABEV3", 5m) // soma = 95
        ]);

        _cestaRepoMock.Setup(r => r.ObterAtivaAsync())
            .ReturnsAsync((CestaRecomendacao?)null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CriarCestaAsync(request));
    }

    // ===== RN-016: Percentual > 0% =====

    [Fact]
    public async Task CriarCestaAsync_PercentualZero_LancaException()
    {
        var request = new CriarCestaRequest("Cesta",
        [
            new("PETR4", 40m),
            new("VALE3", 25m),
            new("ITUB4", 20m),
            new("BBDC4", 15m),
            new("ABEV3", 0m) // zero
        ]);

        _cestaRepoMock.Setup(r => r.ObterAtivaAsync())
            .ReturnsAsync((CestaRecomendacao?)null);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CriarCestaAsync(request));
    }

    // ===== CONSULTAS =====

    [Fact]
    public async Task ObterCestaAtivaAsync_Existe_RetornaCesta()
    {
        var cesta = CestaRecomendacao.Criar("Ativa",
        [
            ("PETR4", 20m), ("VALE3", 20m), ("ITUB4", 20m), ("BBDC4", 20m), ("ABEV3", 20m)
        ]);

        _cestaRepoMock.Setup(r => r.ObterAtivaAsync())
            .ReturnsAsync(cesta);

        var result = await _service.ObterCestaAtivaAsync();

        Assert.NotNull(result);
        Assert.Equal("Ativa", result!.Nome);
        Assert.True(result.Ativa);
    }

    [Fact]
    public async Task ObterCestaAtivaAsync_NaoExiste_RetornaNull()
    {
        _cestaRepoMock.Setup(r => r.ObterAtivaAsync())
            .ReturnsAsync((CestaRecomendacao?)null);

        var result = await _service.ObterCestaAtivaAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task ObterHistoricoAsync_RetornaTodasCestas()
    {
        var cestas = new List<CestaRecomendacao>
        {
            CestaRecomendacao.Criar("Cesta1",
            [
                ("PETR4", 20m), ("VALE3", 20m), ("ITUB4", 20m), ("BBDC4", 20m), ("ABEV3", 20m)
            ]),
            CestaRecomendacao.Criar("Cesta2",
            [
                ("PETR4", 30m), ("VALE3", 25m), ("ITUB4", 20m), ("BBDC4", 15m), ("ABEV3", 10m)
            ])
        };

        _cestaRepoMock.Setup(r => r.ObterHistoricoAsync())
            .ReturnsAsync(cestas);

        var result = await _service.ObterHistoricoAsync();

        Assert.Equal(2, result.Count);
    }
}
