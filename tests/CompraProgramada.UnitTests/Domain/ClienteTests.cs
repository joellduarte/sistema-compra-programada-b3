using CompraProgramada.Domain.Entities;

namespace CompraProgramada.UnitTests.Domain;

public class ClienteTests
{
    private const string CpfValido = "52998224725";

    [Fact]
    public void Criar_DadosValidos_CriaCliente()
    {
        var cliente = Cliente.Criar("João", CpfValido, "joao@email.com", 3000m);

        Assert.Equal("João", cliente.Nome);
        Assert.Equal(CpfValido, cliente.CPF.Numero);
        Assert.Equal(3000m, cliente.ValorMensal);
        Assert.True(cliente.Ativo);
        Assert.Null(cliente.DataSaida);
    }

    [Fact]
    public void Criar_ValorAbaixoMinimo_LancaException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => Cliente.Criar("João", CpfValido, "joao@email.com", 50m));
        Assert.Contains("R$ 100,00", ex.Message);
    }

    [Fact]
    public void Criar_NomeVazio_LancaException()
    {
        Assert.Throws<ArgumentException>(
            () => Cliente.Criar("", CpfValido, "joao@email.com", 500m));
    }

    [Fact]
    public void Criar_EmailVazio_LancaException()
    {
        Assert.Throws<ArgumentException>(
            () => Cliente.Criar("João", CpfValido, "", 500m));
    }

    [Fact]
    public void Sair_ClienteAtivo_DesativaCliente()
    {
        var cliente = Cliente.Criar("João", CpfValido, "joao@email.com", 1000m);
        cliente.Sair();

        Assert.False(cliente.Ativo);
        Assert.NotNull(cliente.DataSaida);
    }

    [Fact]
    public void Sair_ClienteJaInativo_LancaException()
    {
        var cliente = Cliente.Criar("João", CpfValido, "joao@email.com", 1000m);
        cliente.Sair();

        Assert.Throws<InvalidOperationException>(() => cliente.Sair());
    }

    [Fact]
    public void AlterarValorMensal_ValorValido_RetornaValorAnterior()
    {
        var cliente = Cliente.Criar("João", CpfValido, "joao@email.com", 1000m);
        var anterior = cliente.AlterarValorMensal(5000m);

        Assert.Equal(1000m, anterior);
        Assert.Equal(5000m, cliente.ValorMensal);
    }

    [Fact]
    public void AlterarValorMensal_ValorAbaixoMinimo_LancaException()
    {
        var cliente = Cliente.Criar("João", CpfValido, "joao@email.com", 1000m);
        Assert.Throws<ArgumentException>(() => cliente.AlterarValorMensal(50m));
    }

    [Fact]
    public void CalcularValorParcela_RetornaUmTerco()
    {
        var cliente = Cliente.Criar("João", CpfValido, "joao@email.com", 3000m);
        Assert.Equal(1000m, cliente.CalcularValorParcela());
    }

    [Fact]
    public void CalcularValorParcela_ValorNaoExato_Arredonda()
    {
        var cliente = Cliente.Criar("João", CpfValido, "joao@email.com", 1000m);
        // 1000 / 3 = 333.33...
        Assert.Equal(333.33m, cliente.CalcularValorParcela());
    }
}
