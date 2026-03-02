using CompraProgramada.Application.Interfaces;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Moq;

namespace CompraProgramada.UnitTests.Services;

public class EventoIRServiceTests
{
    private readonly Mock<IEventoIRRepository> _eventoIRRepoMock = new();
    private readonly Mock<IKafkaProducer> _kafkaProducerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly EventoIRService _service;

    public EventoIRServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _service = new EventoIRService(
            _eventoIRRepoMock.Object,
            _kafkaProducerMock.Object,
            _unitOfWorkMock.Object);
    }

    // ===== Dedo-Duro: Kafka OK → PublicadoKafka = true =====

    [Fact]
    public async Task RegistrarDedoDuro_KafkaOk_MarcaComoPublicado()
    {
        _kafkaProducerMock.Setup(k => k.PublicarAsync(
                "ir-dedo-duro", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await _service.RegistrarDedoDuroAsync(1, "52998224725", "PETR4", 8, 35m);

        // Deve salvar o evento
        _eventoIRRepoMock.Verify(r => r.AdicionarAsync(It.IsAny<EventoIR>()), Times.Once);
        // Deve atualizar com PublicadoKafka = true
        _eventoIRRepoMock.Verify(r => r.AtualizarAsync(
            It.Is<EventoIR>(e => e.PublicadoKafka)), Times.Once);
        // 2 commits: 1 salvar evento, 1 atualizar publicado
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // ===== Dedo-Duro: Kafka FALHA → PublicadoKafka = false =====

    [Fact]
    public async Task RegistrarDedoDuro_KafkaFalha_NaoMarcaComoPublicado()
    {
        _kafkaProducerMock.Setup(k => k.PublicarAsync(
                "ir-dedo-duro", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false); // Kafka falhou!

        await _service.RegistrarDedoDuroAsync(1, "52998224725", "PETR4", 8, 35m);

        // Deve salvar o evento no banco (registro existe)
        _eventoIRRepoMock.Verify(r => r.AdicionarAsync(It.IsAny<EventoIR>()), Times.Once);
        // NÃO deve atualizar (PublicadoKafka permanece false)
        _eventoIRRepoMock.Verify(r => r.AtualizarAsync(It.IsAny<EventoIR>()), Times.Never);
        // Apenas 1 commit (salvar evento), não o segundo
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===== Dedo-Duro: Publica no tópico correto =====

    [Fact]
    public async Task RegistrarDedoDuro_PublicaNoTopicoCorreto()
    {
        _kafkaProducerMock.Setup(k => k.PublicarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await _service.RegistrarDedoDuroAsync(1, "52998224725", "PETR4", 10, 35m);

        _kafkaProducerMock.Verify(k => k.PublicarAsync(
            "ir-dedo-duro",
            "1-PETR4",
            It.Is<string>(m => m.Contains("DEDO_DURO") && m.Contains("PETR4"))),
            Times.Once);
    }

    // ===== IR Venda: Kafka OK → PublicadoKafka = true =====

    [Fact]
    public async Task RegistrarIRVenda_KafkaOk_MarcaComoPublicado()
    {
        _kafkaProducerMock.Setup(k => k.PublicarAsync(
                "ir-venda", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await _service.RegistrarIRVendaAsync(1, "52998224725", 25_000m, 5_000m);

        _eventoIRRepoMock.Verify(r => r.AdicionarAsync(It.IsAny<EventoIR>()), Times.Once);
        _eventoIRRepoMock.Verify(r => r.AtualizarAsync(
            It.Is<EventoIR>(e => e.PublicadoKafka)), Times.Once);
    }

    // ===== IR Venda: Kafka FALHA → PublicadoKafka = false =====

    [Fact]
    public async Task RegistrarIRVenda_KafkaFalha_NaoMarcaComoPublicado()
    {
        _kafkaProducerMock.Setup(k => k.PublicarAsync(
                "ir-venda", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        await _service.RegistrarIRVendaAsync(1, "52998224725", 25_000m, 5_000m);

        _eventoIRRepoMock.Verify(r => r.AtualizarAsync(It.IsAny<EventoIR>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ===== IR Venda: Publica no tópico correto =====

    [Fact]
    public async Task RegistrarIRVenda_PublicaNoTopicoCorreto()
    {
        _kafkaProducerMock.Setup(k => k.PublicarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await _service.RegistrarIRVendaAsync(1, "52998224725", 25_000m, 5_000m);

        _kafkaProducerMock.Verify(k => k.PublicarAsync(
            "ir-venda",
            "1",
            It.Is<string>(m => m.Contains("IR_VENDA"))),
            Times.Once);
    }

    // ===== Reprocessar pendentes =====

    [Fact]
    public async Task ReprocessarPendentes_PublicaEMarcaComSucesso()
    {
        var eventoPendente = EventoIR.CriarDedoDuro(1, "PETR4", 280m);

        _eventoIRRepoMock.Setup(r => r.ObterNaoPublicadosAsync())
            .ReturnsAsync(new List<EventoIR> { eventoPendente });
        _kafkaProducerMock.Setup(k => k.PublicarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await _service.ReprocessarPendentesAsync();

        Assert.True(eventoPendente.PublicadoKafka);
        _eventoIRRepoMock.Verify(r => r.AtualizarAsync(eventoPendente), Times.Once);
    }

    [Fact]
    public async Task ReprocessarPendentes_KafkaFalha_NaoMarca()
    {
        var eventoPendente = EventoIR.CriarDedoDuro(1, "PETR4", 280m);

        _eventoIRRepoMock.Setup(r => r.ObterNaoPublicadosAsync())
            .ReturnsAsync(new List<EventoIR> { eventoPendente });
        _kafkaProducerMock.Setup(k => k.PublicarAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        await _service.ReprocessarPendentesAsync();

        Assert.False(eventoPendente.PublicadoKafka);
        _eventoIRRepoMock.Verify(r => r.AtualizarAsync(It.IsAny<EventoIR>()), Times.Never);
    }
}
