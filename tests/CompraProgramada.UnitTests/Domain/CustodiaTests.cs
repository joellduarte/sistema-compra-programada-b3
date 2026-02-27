using CompraProgramada.Domain.Entities;

namespace CompraProgramada.UnitTests.Domain;

public class CustodiaTests
{
    [Fact]
    public void AdicionarAcoes_PrimeiraCompra_CalculaPrecoMedio()
    {
        var custodia = Custodia.Criar(1, "PETR4");
        custodia.AdicionarAcoes(10, 35.00m);

        Assert.Equal(10, custodia.Quantidade);
        Assert.Equal(35.00m, custodia.PrecoMedio);
    }

    [Fact]
    public void AdicionarAcoes_SegundaCompra_RecalculaPrecoMedio()
    {
        var custodia = Custodia.Criar(1, "PETR4");
        custodia.AdicionarAcoes(10, 30.00m); // 300
        custodia.AdicionarAcoes(10, 40.00m); // 400

        Assert.Equal(20, custodia.Quantidade);
        Assert.Equal(35.00m, custodia.PrecoMedio); // (300 + 400) / 20 = 35
    }

    [Fact]
    public void RemoverAcoes_NaoAlteraPrecoMedio()
    {
        var custodia = Custodia.Criar(1, "PETR4");
        custodia.AdicionarAcoes(20, 35.00m);

        custodia.RemoverAcoes(5, 40.00m);

        Assert.Equal(15, custodia.Quantidade);
        Assert.Equal(35.00m, custodia.PrecoMedio); // RN-043: PM não altera em venda
    }

    [Fact]
    public void RemoverAcoes_RetornaLucro()
    {
        var custodia = Custodia.Criar(1, "PETR4");
        custodia.AdicionarAcoes(10, 30.00m);

        var lucro = custodia.RemoverAcoes(5, 40.00m);

        Assert.Equal(50m, lucro); // 5 * (40 - 30) = 50
    }

    [Fact]
    public void RemoverAcoes_QuantidadeInsuficiente_LancaException()
    {
        var custodia = Custodia.Criar(1, "PETR4");
        custodia.AdicionarAcoes(5, 30.00m);

        Assert.Throws<InvalidOperationException>(() => custodia.RemoverAcoes(10, 40.00m));
    }

    [Fact]
    public void RemoverAcoes_ZeraPosicao_ResetaPrecoMedio()
    {
        var custodia = Custodia.Criar(1, "PETR4");
        custodia.AdicionarAcoes(10, 35.00m);
        custodia.RemoverAcoes(10, 40.00m);

        Assert.Equal(0, custodia.Quantidade);
        Assert.Equal(0m, custodia.PrecoMedio);
    }

    [Fact]
    public void CalcularPL_RetornaDiferenca()
    {
        var custodia = Custodia.Criar(1, "PETR4");
        custodia.AdicionarAcoes(10, 30.00m);

        var pl = custodia.CalcularPL(35.00m);

        Assert.Equal(50m, pl); // 10 * (35 - 30) = 50
    }

    [Fact]
    public void CalcularValorAtual_RetornaQuantidadeVezesCotacao()
    {
        var custodia = Custodia.Criar(1, "PETR4");
        custodia.AdicionarAcoes(10, 30.00m);

        Assert.Equal(350m, custodia.CalcularValorAtual(35.00m));
    }
}
