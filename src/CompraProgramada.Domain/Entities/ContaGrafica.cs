using CompraProgramada.Domain.Enums;

namespace CompraProgramada.Domain.Entities;

public class ContaGrafica : EntityBase
{
    public long? ClienteId { get; private set; }
    public string NumeroConta { get; private set; } = string.Empty;
    public TipoConta Tipo { get; private set; }
    public DateTime DataCriacao { get; private set; }

    // Navegação
    public Cliente? Cliente { get; private set; }
    public IReadOnlyCollection<Custodia> Custodias => _custodias.AsReadOnly();
    private readonly List<Custodia> _custodias = [];

    private ContaGrafica() { } // EF Core

    /// <summary>
    /// Cria conta gráfica filhote vinculada a um cliente (RN-004).
    /// </summary>
    public static ContaGrafica CriarFilhote(long clienteId, string numeroConta)
    {
        return new ContaGrafica
        {
            ClienteId = clienteId,
            NumeroConta = numeroConta,
            Tipo = TipoConta.Filhote,
            DataCriacao = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Cria a conta master da corretora (única no sistema).
    /// </summary>
    public static ContaGrafica CriarMaster(string numeroConta = "MST-000001")
    {
        return new ContaGrafica
        {
            ClienteId = null,
            NumeroConta = numeroConta,
            Tipo = TipoConta.Master,
            DataCriacao = DateTime.UtcNow
        };
    }
}
