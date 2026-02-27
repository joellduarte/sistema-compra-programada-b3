using CompraProgramada.Domain.Enums;

namespace CompraProgramada.Domain.Entities;

public class OrdemCompra : EntityBase
{
    public long ContaMasterId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoUnitario { get; private set; }
    public TipoMercado TipoMercado { get; private set; }
    public DateTime DataExecucao { get; private set; }
    public DateTime DataReferencia { get; private set; }

    // Navegação
    public ContaGrafica ContaMaster { get; private set; } = null!;
    public IReadOnlyCollection<Distribuicao> Distribuicoes => _distribuicoes.AsReadOnly();
    private readonly List<Distribuicao> _distribuicoes = [];

    private OrdemCompra() { } // EF Core

    /// <summary>
    /// RN-031 a RN-033: Cria ordem de compra (lote padrão ou fracionário).
    /// </summary>
    public static OrdemCompra Criar(
        long contaMasterId,
        string ticker,
        int quantidade,
        decimal precoUnitario,
        TipoMercado tipoMercado,
        DateTime dataReferencia)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.");

        return new OrdemCompra
        {
            ContaMasterId = contaMasterId,
            Ticker = ticker.Trim().ToUpperInvariant(),
            Quantidade = quantidade,
            PrecoUnitario = precoUnitario,
            TipoMercado = tipoMercado,
            DataExecucao = DateTime.UtcNow,
            DataReferencia = dataReferencia
        };
    }

    /// <summary>
    /// Retorna o ticker de negociação (com sufixo F se fracionário - RN-033).
    /// </summary>
    public string ObterTickerNegociacao()
    {
        return TipoMercado == TipoMercado.Fracionario
            ? $"{Ticker}F"
            : Ticker;
    }

    /// <summary>
    /// Calcula o valor total da ordem.
    /// </summary>
    public decimal CalcularValorTotal() => Quantidade * PrecoUnitario;

    /// <summary>
    /// RN-031/RN-032: Separa quantidade em lotes e frações.
    /// Retorna (quantidadeLote, quantidadeFracionario).
    /// </summary>
    public static (int Lote, int Fracionario) SepararLoteEFracionario(int quantidadeTotal)
    {
        var lotes = (quantidadeTotal / 100) * 100;
        var fracionario = quantidadeTotal % 100;
        return (lotes, fracionario);
    }
}
