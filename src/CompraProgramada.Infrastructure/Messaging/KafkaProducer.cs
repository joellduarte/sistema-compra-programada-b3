using CompraProgramada.Application.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Infrastructure.Messaging;

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        _logger = logger;

        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092",
            Acks = Acks.All, // Espera confirmação de todas as réplicas
            EnableIdempotence = true, // Garante exatamente uma entrega
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 1000
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task<bool> PublicarAsync(string topico, string chave, string mensagemJson)
    {
        try
        {
            var result = await _producer.ProduceAsync(topico, new Message<string, string>
            {
                Key = chave,
                Value = mensagemJson
            });

            _logger.LogInformation(
                "Kafka: Mensagem publicada no tópico {Topico}, partição {Particao}, offset {Offset}",
                result.Topic, result.Partition.Value, result.Offset.Value);

            return result.Status == PersistenceStatus.Persisted;
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex,
                "Kafka: Falha ao publicar no tópico {Topico}. Erro: {Erro}",
                topico, ex.Error.Reason);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Kafka: Erro inesperado ao publicar no tópico {Topico}", topico);
            return false;
        }
    }

    public void Dispose()
    {
        _producer?.Flush(TimeSpan.FromSeconds(5));
        _producer?.Dispose();
    }
}
