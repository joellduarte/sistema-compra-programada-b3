namespace CompraProgramada.Domain.Entities;

public class ItemCesta : EntityBase
{
    public long CestaId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public decimal Percentual { get; private set; }

    // Navegação
    public CestaRecomendacao Cesta { get; private set; } = null!;

    private ItemCesta() { } // EF Core

    internal static ItemCesta Criar(string ticker, decimal percentual)
    {
        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("Ticker é obrigatório.");

        return new ItemCesta
        {
            Ticker = ticker.Trim().ToUpperInvariant(),
            Percentual = percentual
        };
    }
}
