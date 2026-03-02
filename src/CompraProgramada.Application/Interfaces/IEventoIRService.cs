using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Application.Interfaces;

public interface IEventoIRService
{
    /// <summary>
    /// RN-053/054: Cria evento IR dedo-duro para uma distribuição e publica no Kafka.
    /// </summary>
    Task RegistrarDedoDuroAsync(long clienteId, string cpf, string ticker,
        int quantidade, decimal precoUnitario);

    /// <summary>
    /// RN-057 a RN-062: Cria evento IR sobre vendas (se aplicável) e publica no Kafka.
    /// </summary>
    Task RegistrarIRVendaAsync(long clienteId, string cpf,
        decimal totalVendasMes, decimal lucroLiquido);

    /// <summary>
    /// Reprocessa eventos que falharam na publicação Kafka.
    /// </summary>
    Task ReprocessarPendentesAsync();
}
