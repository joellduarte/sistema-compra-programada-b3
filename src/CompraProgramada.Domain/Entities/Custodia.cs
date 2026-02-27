namespace CompraProgramada.Domain.Entities;

public class Custodia : EntityBase
{
    public long ContaGraficaId { get; private set; }
    public string Ticker { get; private set; } = string.Empty;
    public int Quantidade { get; private set; }
    public decimal PrecoMedio { get; private set; }
    public DateTime DataUltimaAtualizacao { get; private set; }

    // Navegação
    public ContaGrafica ContaGrafica { get; private set; } = null!;

    private Custodia() { } // EF Core

    public static Custodia Criar(long contaGraficaId, string ticker)
    {
        return new Custodia
        {
            ContaGraficaId = contaGraficaId,
            Ticker = ticker.Trim().ToUpperInvariant(),
            Quantidade = 0,
            PrecoMedio = 0m,
            DataUltimaAtualizacao = DateTime.UtcNow
        };
    }

    /// <summary>
    /// RN-042/RN-044: Adiciona ações e recalcula preço médio.
    /// PM = (Qtd Anterior × PM Anterior + Qtd Nova × Preço Novo) / (Qtd Anterior + Qtd Nova)
    /// </summary>
    public void AdicionarAcoes(int quantidade, decimal precoUnitario)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.");
        if (precoUnitario <= 0)
            throw new ArgumentException("Preço unitário deve ser maior que zero.");

        var valorAnterior = Quantidade * PrecoMedio;
        var valorNovo = quantidade * precoUnitario;
        var quantidadeTotal = Quantidade + quantidade;

        PrecoMedio = quantidadeTotal > 0
            ? (valorAnterior + valorNovo) / quantidadeTotal
            : 0m;

        Quantidade = quantidadeTotal;
        DataUltimaAtualizacao = DateTime.UtcNow;
    }

    /// <summary>
    /// RN-043: Remove ações sem alterar o preço médio.
    /// Retorna o lucro/prejuízo da operação.
    /// </summary>
    public decimal RemoverAcoes(int quantidade, decimal precoVenda)
    {
        if (quantidade <= 0)
            throw new ArgumentException("Quantidade deve ser maior que zero.");
        if (quantidade > Quantidade)
            throw new InvalidOperationException(
                $"Quantidade insuficiente. Disponível: {Quantidade}, Solicitado: {quantidade}");

        var lucro = quantidade * (precoVenda - PrecoMedio);

        Quantidade -= quantidade;
        DataUltimaAtualizacao = DateTime.UtcNow;

        // Se zerou a posição, reseta o preço médio
        if (Quantidade == 0)
            PrecoMedio = 0m;

        return lucro;
    }

    /// <summary>
    /// Calcula o valor de mercado atual desta posição.
    /// </summary>
    public decimal CalcularValorAtual(decimal cotacaoAtual) => Quantidade * cotacaoAtual;

    /// <summary>
    /// RN-064: Calcula P/L (Profit/Loss) da posição.
    /// </summary>
    public decimal CalcularPL(decimal cotacaoAtual) => Quantidade * (cotacaoAtual - PrecoMedio);
}
