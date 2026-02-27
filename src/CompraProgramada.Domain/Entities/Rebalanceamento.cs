using CompraProgramada.Domain.Enums;

namespace CompraProgramada.Domain.Entities;

public class Rebalanceamento : EntityBase
{
    public long ClienteId { get; private set; }
    public TipoRebalanceamento Tipo { get; private set; }
    public string TickerVendido { get; private set; } = string.Empty;
    public int QuantidadeVendida { get; private set; }
    public decimal PrecoVenda { get; private set; }
    public decimal ValorVenda { get; private set; }
    public string? TickerComprado { get; private set; }
    public int QuantidadeComprada { get; private set; }
    public decimal PrecoCompra { get; private set; }
    public decimal ValorCompra { get; private set; }
    public DateTime DataRebalanceamento { get; private set; }

    // Navegação
    public Cliente Cliente { get; private set; } = null!;

    private Rebalanceamento() { } // EF Core

    /// <summary>
    /// RN-045 a RN-049: Cria registro de rebalanceamento.
    /// </summary>
    public static Rebalanceamento Criar(
        long clienteId,
        TipoRebalanceamento tipo,
        string tickerVendido,
        int quantidadeVendida,
        decimal precoVenda,
        string? tickerComprado,
        int quantidadeComprada,
        decimal precoCompra)
    {
        return new Rebalanceamento
        {
            ClienteId = clienteId,
            Tipo = tipo,
            TickerVendido = tickerVendido.Trim().ToUpperInvariant(),
            QuantidadeVendida = quantidadeVendida,
            PrecoVenda = precoVenda,
            ValorVenda = quantidadeVendida * precoVenda,
            TickerComprado = tickerComprado?.Trim().ToUpperInvariant(),
            QuantidadeComprada = quantidadeComprada,
            PrecoCompra = precoCompra,
            ValorCompra = quantidadeComprada * precoCompra,
            DataRebalanceamento = DateTime.UtcNow
        };
    }
}
