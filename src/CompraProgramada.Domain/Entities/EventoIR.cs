using CompraProgramada.Domain.Enums;

namespace CompraProgramada.Domain.Entities;

public class EventoIR : EntityBase
{
    public long ClienteId { get; private set; }
    public TipoEventoIR Tipo { get; private set; }
    public string? Ticker { get; private set; }
    public decimal ValorBase { get; private set; }
    public decimal Aliquota { get; private set; }
    public decimal ValorIR { get; private set; }
    public bool PublicadoKafka { get; private set; }
    public DateTime DataEvento { get; private set; }

    // Navegação
    public Cliente Cliente { get; private set; } = null!;

    private EventoIR() { } // EF Core

    /// <summary>
    /// RN-053 a RN-056: Cria evento de IR dedo-duro.
    /// </summary>
    public static EventoIR CriarDedoDuro(long clienteId, string ticker, decimal valorOperacao)
    {
        const decimal aliquota = 0.00005m; // 0,005%
        var valorIR = Math.Round(valorOperacao * aliquota, 2);

        return new EventoIR
        {
            ClienteId = clienteId,
            Tipo = TipoEventoIR.DedoDuro,
            Ticker = ticker.Trim().ToUpperInvariant(),
            ValorBase = valorOperacao,
            Aliquota = aliquota,
            ValorIR = valorIR,
            PublicadoKafka = false,
            DataEvento = DateTime.UtcNow
        };
    }

    /// <summary>
    /// RN-057 a RN-062: Cria evento de IR sobre vendas (rebalanceamento).
    /// </summary>
    public static EventoIR CriarIRVenda(long clienteId, decimal totalVendasMes, decimal lucroLiquido)
    {
        const decimal aliquota = 0.20m; // 20%

        // RN-058: se vendas <= 20k, isento
        // RN-059: se vendas > 20k, 20% sobre lucro
        // RN-061: se prejuízo, IR = 0
        var valorIR = 0m;
        if (totalVendasMes > 20_000m && lucroLiquido > 0)
        {
            valorIR = Math.Round(lucroLiquido * aliquota, 2);
        }

        return new EventoIR
        {
            ClienteId = clienteId,
            Tipo = TipoEventoIR.IRVenda,
            Ticker = null,
            ValorBase = totalVendasMes,
            Aliquota = totalVendasMes > 20_000m ? aliquota : 0m,
            ValorIR = valorIR,
            PublicadoKafka = false,
            DataEvento = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Marca como publicado no Kafka com sucesso.
    /// </summary>
    public void MarcarPublicado()
    {
        PublicadoKafka = true;
    }
}
