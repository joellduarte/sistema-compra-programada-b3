using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Enums;

namespace CompraProgramada.UnitTests.Domain;

public class EventoIRTests
{
    // ===== RN-053/054: IR Dedo-Duro =====

    [Fact]
    public void CriarDedoDuro_CalculaAliquotaCorreta()
    {
        // 8 ações PETR4 x R$35 = R$280, IR = 280 * 0,005% = 0,014 → R$0,01
        var evento = EventoIR.CriarDedoDuro(1, "PETR4", 280m);

        Assert.Equal(TipoEventoIR.DedoDuro, evento.Tipo);
        Assert.Equal("PETR4", evento.Ticker);
        Assert.Equal(280m, evento.ValorBase);
        Assert.Equal(0.00005m, evento.Aliquota);
        Assert.Equal(0.01m, evento.ValorIR); // 280 * 0.00005 = 0.014 → arredonda para 0.01
        Assert.False(evento.PublicadoKafka);
    }

    [Fact]
    public void CriarDedoDuro_ValorAlto_CalculaCorretamente()
    {
        // 100 ações x R$50 = R$5000, IR = 5000 * 0.00005 = 0.25
        var evento = EventoIR.CriarDedoDuro(1, "VALE3", 5000m);

        Assert.Equal(0.25m, evento.ValorIR);
    }

    [Fact]
    public void CriarDedoDuro_TickerConvertidoUpperCase()
    {
        var evento = EventoIR.CriarDedoDuro(1, "petr4", 100m);
        Assert.Equal("PETR4", evento.Ticker);
    }

    // ===== RN-057 a RN-062: IR sobre Vendas =====

    [Fact]
    public void CriarIRVenda_VendasAbaixo20k_Isento()
    {
        // Vendas R$15.000, lucro R$2.000 → isento (< R$20k)
        var evento = EventoIR.CriarIRVenda(1, 15_000m, 2_000m);

        Assert.Equal(TipoEventoIR.IRVenda, evento.Tipo);
        Assert.Equal(0m, evento.ValorIR);
        Assert.Equal(0m, evento.Aliquota);
    }

    [Fact]
    public void CriarIRVenda_VendasAcima20k_Calcula20Porcento()
    {
        // Vendas R$25.000, lucro R$5.000 → IR = 5000 * 20% = R$1.000
        var evento = EventoIR.CriarIRVenda(1, 25_000m, 5_000m);

        Assert.Equal(0.20m, evento.Aliquota);
        Assert.Equal(1_000m, evento.ValorIR);
    }

    [Fact]
    public void CriarIRVenda_VendasAcima20k_Prejuizo_IRZero()
    {
        // Vendas R$25.000, prejuízo -R$1.000 → IR = 0
        var evento = EventoIR.CriarIRVenda(1, 25_000m, -1_000m);

        Assert.Equal(0m, evento.ValorIR);
    }

    [Fact]
    public void CriarIRVenda_VendasExato20k_Isento()
    {
        // Vendas = R$20.000 (não é "maior que") → isento
        var evento = EventoIR.CriarIRVenda(1, 20_000m, 3_000m);

        Assert.Equal(0m, evento.ValorIR);
    }

    // ===== MarcarPublicado =====

    [Fact]
    public void MarcarPublicado_AlteraFlag()
    {
        var evento = EventoIR.CriarDedoDuro(1, "PETR4", 100m);
        Assert.False(evento.PublicadoKafka);

        evento.MarcarPublicado();

        Assert.True(evento.PublicadoKafka);
    }
}
