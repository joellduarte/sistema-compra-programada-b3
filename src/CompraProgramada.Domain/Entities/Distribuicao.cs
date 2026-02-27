namespace CompraProgramada.Domain.Entities;

public class Distribuicao : EntityBase
{
    public long OrdemCompraId { get; private set; }
    public long CustodiaFilhoteId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public DateTime DataDistribuicao { get; private set; }

    // Navegação
    public OrdemCompra OrdemCompra { get; private set; } = null!;
    public Custodia CustodiaFilhote { get; private set; } = null!;

    private Distribuicao() { } // EF Core

    /// <summary>
    /// RN-034 a RN-038: Cria registro de distribuição para uma custódia filhote.
    /// </summary>
    public static Distribuicao Criar(
        long ordemCompraId,
        long custodiaFilhoteId,
        string ticker,
        int quantidade,
        decimal precoUnitario)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.");

        return new Distribuicao
        {
            OrdemCompraId = ordemCompraId,
            CustodiaFilhoteId = custodiaFilhoteId,
            Ticker = ticker.Trim().ToUpperInvariant(),
            Quantidade = quantidade,
            PrecoUnitario = precoUnitario,
            DataDistribuicao = DateTime.UtcNow
        };
    }

    /// <summary>
    /// RN-053: Calcula IR dedo-duro (0,005% sobre valor da operação).
    /// </summary>
    public decimal CalcularIRDedoDuro()
    {
        var valorOperacao = Quantidade * PrecoUnitario;
        return Math.Round(valorOperacao * 0.00005m, 2);
    }
}
