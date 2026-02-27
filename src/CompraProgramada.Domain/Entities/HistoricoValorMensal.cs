namespace CompraProgramada.Domain.Entities;

/// <summary>
/// RN-013: Mantém histórico de alterações do valor mensal do cliente.
/// </summary>
public class HistoricoValorMensal : EntityBase
{
    public long ClienteId { get; private set; }
    public decimal ValorAnterior { get; private set; }
    public decimal ValorNovo { get; private set; }
    public DateTime DataAlteracao { get; private set; }

    // Navegação
    public Cliente Cliente { get; private set; } = null!;

    private HistoricoValorMensal() { } // EF Core

    public static HistoricoValorMensal Criar(long clienteId, decimal valorAnterior, decimal valorNovo)
    {
        return new HistoricoValorMensal
        {
            ClienteId = clienteId,
            ValorAnterior = valorAnterior,
            ValorNovo = valorNovo,
            DataAlteracao = DateTime.UtcNow
        };
    }
}
