namespace CompraProgramada.Domain.Entities;

public class Cotacao : EntityBase
{
    public DateTime DataPregao { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public string CodigoBDI { get; private set; } = string.Empty;
    public int TipoMercado { get; private set; }
    public string NomeEmpresa { get; private set; } = string.Empty;
    public decimal PrecoAbertura { get; private set; }
    public decimal PrecoFechamento { get; private set; }
    public decimal PrecoMaximo { get; private set; }
    public decimal PrecoMinimo { get; private set; }
    public decimal PrecoMedio { get; private set; }
    public long QuantidadeNegociada { get; private set; }
    public decimal VolumeNegociado { get; private set; }

    private Cotacao() { } // EF Core

    public static Cotacao Criar(
        DateTime dataPregao,
        string ticker,
        string codigoBDI,
        int tipoMercado,
        string nomeEmpresa,
        decimal precoAbertura,
        decimal precoFechamento,
        decimal precoMaximo,
        decimal precoMinimo,
        decimal precoMedio,
        long quantidadeNegociada,
        decimal volumeNegociado)
    {
        return new Cotacao
        {
            DataPregao = dataPregao,
            Ticker = ticker.Trim().ToUpperInvariant(),
            CodigoBDI = codigoBDI.Trim(),
            TipoMercado = tipoMercado,
            NomeEmpresa = nomeEmpresa.Trim(),
            PrecoAbertura = precoAbertura,
            PrecoFechamento = precoFechamento,
            PrecoMaximo = precoMaximo,
            PrecoMinimo = precoMinimo,
            PrecoMedio = precoMedio,
            QuantidadeNegociada = quantidadeNegociada,
            VolumeNegociado = volumeNegociado
        };
    }

    /// <summary>
    /// Verifica se é mercado à vista (010).
    /// </summary>
    public bool IsMercadoAVista() => TipoMercado == 10;

    /// <summary>
    /// Verifica se é mercado fracionário (020).
    /// </summary>
    public bool IsMercadoFracionario() => TipoMercado == 20;
}
