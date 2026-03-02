namespace CompraProgramada.Application.Interfaces;

/// <summary>
/// Abstração do producer Kafka para permitir mock em testes.
/// </summary>
public interface IKafkaProducer
{
    /// <summary>
    /// Publica uma mensagem no tópico especificado.
    /// Retorna true se o broker confirmou (ack), false se falhou.
    /// </summary>
    Task<bool> PublicarAsync(string topico, string chave, string mensagemJson);
}
