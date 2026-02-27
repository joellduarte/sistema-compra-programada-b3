namespace CompraProgramada.Domain.Entities;

public class CestaRecomendacao : EntityBase
{
    public string Nome { get; private set; } = string.Empty;
    public bool Ativa { get; private set; }
    public DateTime DataCriacao { get; private set; }
    public DateTime? DataDesativacao { get; private set; }

    // Navegação
    public IReadOnlyCollection<ItemCesta> Itens => _itens.AsReadOnly();
    private readonly List<ItemCesta> _itens = [];

    private CestaRecomendacao() { } // EF Core

    /// <summary>
    /// RN-014 a RN-018: Cria uma nova cesta com exatamente 5 ações somando 100%.
    /// </summary>
    public static CestaRecomendacao Criar(string nome, IEnumerable<(string Ticker, decimal Percentual)> itens)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome da cesta é obrigatório.");

        var listaItens = itens.ToList();

        // RN-014: exatamente 5 ações
        if (listaItens.Count != 5)
            throw new ArgumentException(
                $"A cesta deve conter exatamente 5 ativos. Quantidade informada: {listaItens.Count}.");

        // RN-016: cada percentual > 0
        if (listaItens.Any(i => i.Percentual <= 0))
            throw new ArgumentException("Cada percentual deve ser maior que 0%.");

        // RN-015: soma = 100%
        var soma = listaItens.Sum(i => i.Percentual);
        if (Math.Abs(soma - 100m) > 0.01m)
            throw new ArgumentException(
                $"A soma dos percentuais deve ser exatamente 100%. Soma atual: {soma}%.");

        var cesta = new CestaRecomendacao
        {
            Nome = nome.Trim(),
            Ativa = true,
            DataCriacao = DateTime.UtcNow
        };

        foreach (var (ticker, percentual) in listaItens)
        {
            cesta._itens.Add(ItemCesta.Criar(ticker, percentual));
        }

        return cesta;
    }

    /// <summary>
    /// RN-017: Desativa a cesta (quando uma nova é criada).
    /// </summary>
    public void Desativar()
    {
        Ativa = false;
        DataDesativacao = DateTime.UtcNow;
    }

    /// <summary>
    /// Retorna os tickers desta cesta.
    /// </summary>
    public IReadOnlySet<string> ObterTickers()
        => _itens.Select(i => i.Ticker).ToHashSet();

    /// <summary>
    /// Retorna o percentual de um ticker nesta cesta.
    /// </summary>
    public decimal ObterPercentual(string ticker)
        => _itens.FirstOrDefault(i =>
            i.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase))?.Percentual ?? 0m;
}
