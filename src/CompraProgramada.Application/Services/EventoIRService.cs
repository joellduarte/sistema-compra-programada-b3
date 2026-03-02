using System.Text.Json;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;

namespace CompraProgramada.Application.Services;

public class EventoIRService : IEventoIRService
{
    private readonly IEventoIRRepository _eventoIRRepository;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IUnitOfWork _unitOfWork;

    private const string TopicoDedoDuro = "ir-dedo-duro";
    private const string TopicoIRVenda = "ir-venda";

    public EventoIRService(
        IEventoIRRepository eventoIRRepository,
        IKafkaProducer kafkaProducer,
        IUnitOfWork unitOfWork)
    {
        _eventoIRRepository = eventoIRRepository;
        _kafkaProducer = kafkaProducer;
        _unitOfWork = unitOfWork;
    }

    public async Task RegistrarDedoDuroAsync(long clienteId, string cpf, string ticker,
        int quantidade, decimal precoUnitario)
    {
        var valorOperacao = quantidade * precoUnitario;

        // RN-053/054: Criar evento IR dedo-duro
        var evento = EventoIR.CriarDedoDuro(clienteId, ticker, valorOperacao);
        await _eventoIRRepository.AdicionarAsync(evento);
        await _unitOfWork.CommitAsync();

        // RN-055/056: Publicar no Kafka com dados completos
        var mensagem = JsonSerializer.Serialize(new
        {
            eventoId = evento.Id,
            clienteId,
            cpf,
            ticker,
            valorOperacao,
            valorIR = evento.ValorIR,
            aliquota = evento.Aliquota,
            dataEvento = evento.DataEvento,
            tipo = "DEDO_DURO"
        });

        var publicado = await _kafkaProducer.PublicarAsync(
            TopicoDedoDuro, $"{clienteId}-{ticker}", mensagem);

        // Só marca como publicado se o broker confirmou
        if (publicado)
        {
            evento.MarcarPublicado();
            await _eventoIRRepository.AtualizarAsync(evento);
            await _unitOfWork.CommitAsync();
        }
    }

    public async Task RegistrarIRVendaAsync(long clienteId, string cpf,
        decimal totalVendasMes, decimal lucroLiquido)
    {
        // RN-057 a RN-062: Criar evento IR venda
        var evento = EventoIR.CriarIRVenda(clienteId, totalVendasMes, lucroLiquido);
        await _eventoIRRepository.AdicionarAsync(evento);
        await _unitOfWork.CommitAsync();

        var mensagem = JsonSerializer.Serialize(new
        {
            eventoId = evento.Id,
            clienteId,
            cpf,
            totalVendasMes,
            lucroLiquido,
            valorIR = evento.ValorIR,
            aliquota = evento.Aliquota,
            isento = totalVendasMes <= 20_000m,
            dataEvento = evento.DataEvento,
            tipo = "IR_VENDA"
        });

        var publicado = await _kafkaProducer.PublicarAsync(
            TopicoIRVenda, clienteId.ToString(), mensagem);

        if (publicado)
        {
            evento.MarcarPublicado();
            await _eventoIRRepository.AtualizarAsync(evento);
            await _unitOfWork.CommitAsync();
        }
    }

    public async Task ReprocessarPendentesAsync()
    {
        var pendentes = await _eventoIRRepository.ObterNaoPublicadosAsync();

        foreach (var evento in pendentes)
        {
            var topico = evento.Tipo == Domain.Enums.TipoEventoIR.DedoDuro
                ? TopicoDedoDuro
                : TopicoIRVenda;

            var mensagem = JsonSerializer.Serialize(new
            {
                eventoId = evento.Id,
                clienteId = evento.ClienteId,
                ticker = evento.Ticker,
                valorBase = evento.ValorBase,
                valorIR = evento.ValorIR,
                aliquota = evento.Aliquota,
                dataEvento = evento.DataEvento,
                tipo = evento.Tipo.ToString(),
                reprocessado = true
            });

            var publicado = await _kafkaProducer.PublicarAsync(
                topico, evento.ClienteId.ToString(), mensagem);

            if (publicado)
            {
                evento.MarcarPublicado();
                await _eventoIRRepository.AtualizarAsync(evento);
                await _unitOfWork.CommitAsync();
            }
        }
    }
}
