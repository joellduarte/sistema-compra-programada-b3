using CompraProgramada.Domain.Entities;

namespace CompraProgramada.UnitTests.Domain;

public class CestaRecomendacaoTests
{
    private static List<(string Ticker, decimal Percentual)> CriarItensValidos() =>
    [
        ("PETR4", 30m),
        ("VALE3", 25m),
        ("ITUB4", 20m),
        ("BBDC4", 15m),
        ("ABEV3", 10m)
    ];

    // ===== RN-014: Exatamente 5 ativos =====

    [Fact]
    public void Criar_5Ativos_CriaCestaComSucesso()
    {
        var cesta = CestaRecomendacao.Criar("Top Five Jan/2026", CriarItensValidos());

        Assert.Equal("Top Five Jan/2026", cesta.Nome);
        Assert.True(cesta.Ativa);
        Assert.Equal(5, cesta.Itens.Count);
        Assert.Null(cesta.DataDesativacao);
    }

    [Fact]
    public void Criar_MenosDe5Ativos_LancaException()
    {
        var itens = CriarItensValidos().Take(4).ToList();
        // Ajustar soma para 100%
        itens[3] = ("BBDC4", 25m); // 30+25+20+25 = 100

        var ex = Assert.Throws<ArgumentException>(() =>
            CestaRecomendacao.Criar("Cesta", itens));
        Assert.Contains("exatamente 5", ex.Message);
    }

    [Fact]
    public void Criar_MaisDe5Ativos_LancaException()
    {
        var itens = CriarItensValidos();
        itens.Add(("WEGE3", 5m));

        var ex = Assert.Throws<ArgumentException>(() =>
            CestaRecomendacao.Criar("Cesta", itens));
        Assert.Contains("exatamente 5", ex.Message);
    }

    // ===== RN-015: Soma dos percentuais = 100% =====

    [Fact]
    public void Criar_SomaNao100_LancaException()
    {
        var itens = new List<(string, decimal)>
        {
            ("PETR4", 30m),
            ("VALE3", 25m),
            ("ITUB4", 20m),
            ("BBDC4", 15m),
            ("ABEV3", 5m) // soma = 95
        };

        var ex = Assert.Throws<ArgumentException>(() =>
            CestaRecomendacao.Criar("Cesta", itens));
        Assert.Contains("100%", ex.Message);
    }

    [Fact]
    public void Criar_SomaMaiorQue100_LancaException()
    {
        var itens = new List<(string, decimal)>
        {
            ("PETR4", 30m),
            ("VALE3", 25m),
            ("ITUB4", 20m),
            ("BBDC4", 15m),
            ("ABEV3", 15m) // soma = 105
        };

        var ex = Assert.Throws<ArgumentException>(() =>
            CestaRecomendacao.Criar("Cesta", itens));
        Assert.Contains("100%", ex.Message);
    }

    // ===== RN-016: Cada percentual > 0% =====

    [Fact]
    public void Criar_PercentualZero_LancaException()
    {
        var itens = new List<(string, decimal)>
        {
            ("PETR4", 40m),
            ("VALE3", 25m),
            ("ITUB4", 20m),
            ("BBDC4", 15m),
            ("ABEV3", 0m) // zero
        };

        var ex = Assert.Throws<ArgumentException>(() =>
            CestaRecomendacao.Criar("Cesta", itens));
        Assert.Contains("maior que 0%", ex.Message);
    }

    [Fact]
    public void Criar_PercentualNegativo_LancaException()
    {
        var itens = new List<(string, decimal)>
        {
            ("PETR4", 50m),
            ("VALE3", 25m),
            ("ITUB4", 20m),
            ("BBDC4", 15m),
            ("ABEV3", -10m) // negativo
        };

        var ex = Assert.Throws<ArgumentException>(() =>
            CestaRecomendacao.Criar("Cesta", itens));
        Assert.Contains("maior que 0%", ex.Message);
    }

    // ===== RN-017: Desativar cesta =====

    [Fact]
    public void Desativar_CestaAtiva_MarcaInatvaComData()
    {
        var cesta = CestaRecomendacao.Criar("Cesta", CriarItensValidos());

        cesta.Desativar();

        Assert.False(cesta.Ativa);
        Assert.NotNull(cesta.DataDesativacao);
    }

    // ===== Validações adicionais =====

    [Fact]
    public void Criar_NomeVazio_LancaException()
    {
        Assert.Throws<ArgumentException>(() =>
            CestaRecomendacao.Criar("", CriarItensValidos()));
    }

    [Fact]
    public void ObterTickers_RetornaConjuntoDeTickers()
    {
        var cesta = CestaRecomendacao.Criar("Cesta", CriarItensValidos());

        var tickers = cesta.ObterTickers();

        Assert.Equal(5, tickers.Count);
        Assert.Contains("PETR4", tickers);
        Assert.Contains("VALE3", tickers);
    }

    [Fact]
    public void ObterPercentual_TickerExistente_RetornaValor()
    {
        var cesta = CestaRecomendacao.Criar("Cesta", CriarItensValidos());

        Assert.Equal(30m, cesta.ObterPercentual("PETR4"));
        Assert.Equal(0m, cesta.ObterPercentual("WEGE3")); // não existe
    }

    [Fact]
    public void Criar_TickersConvertidosParaUpperCase()
    {
        var itens = new List<(string, decimal)>
        {
            ("petr4", 30m),
            ("vale3", 25m),
            ("itub4", 20m),
            ("bbdc4", 15m),
            ("abev3", 10m)
        };

        var cesta = CestaRecomendacao.Criar("Cesta", itens);

        Assert.All(cesta.Itens, item => Assert.Equal(item.Ticker, item.Ticker.ToUpperInvariant()));
    }
}
