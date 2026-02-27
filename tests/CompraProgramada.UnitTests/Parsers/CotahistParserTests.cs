using CompraProgramada.Infrastructure.Parsers;

namespace CompraProgramada.UnitTests.Parsers;

public class CotahistParserTests
{
    private readonly CotahistParser _parser = new();

    // Helper: constrói uma linha COTAHIST válida com 245 chars
    // Posições (1-based):
    //  1-2:   TIPREG  (01)
    //  3-10:  DATPRE  (20260226)
    // 11-12:  CODBDI  (02)
    // 13-24:  CODNEG  (PETR4       )
    // 25-27:  TPMERC  (010)
    // 28-39:  NOMRES  (PETROBRAS   )
    // 40-49:  ESPECI  (PN      ED) — 10 chars (não usamos)
    // 50-52:  PRAZOT  (   ) — 3 chars
    // 53-56:  MODREF  (R$  ) — 4 chars
    // 57-69:  PREABE  (0000000003850) — R$ 38.50
    // 70-82:  PREMAX  (0000000003950) — R$ 39.50
    // 83-95:  PREMIN  (0000000003750) — R$ 37.50
    // 96-108: PREMED  (0000000003850) — R$ 38.50
    //109-121: PREULT  (0000000003900) — R$ 39.00
    //122-134: PREOFC  (13 chars)
    //135-147: PREOFV  (13 chars)
    //148-152: TOTNEG  (5 chars)
    //153-170: QUATOT  (000000000000001000) — 1000
    //171-188: VOLTOT  (000000000003850000) — R$ 38500.00
    //189-201: PREEXE  (13 chars)
    //202-202: INDOPC  (1 char)
    //203-210: DATVEN  (8 chars)
    //211-217: FATCOT  (7 chars)
    //218-230: PTOEXE  (13 chars)
    //231-242: CODISI  (12 chars)
    //243-245: DISMES  (3 chars)
    private static string CriarLinhaValida(
        string tipoRegistro = "01",
        string dataPregao = "20260226",
        string codigoBDI = "02",
        string ticker = "PETR4",
        string tipoMercado = "010",
        string nomeEmpresa = "PETROBRAS",
        string precoAbertura = "0000000003850",
        string precoMaximo = "0000000003950",
        string precoMinimo = "0000000003750",
        string precoMedio = "0000000003850",
        string precoFechamento = "0000000003900",
        string quantidade = "000000000000001000",
        string volume = "000000000003850000")
    {
        // Montar a linha posicional de 245 chars
        var linha = new char[245];
        Array.Fill(linha, ' ');

        // TIPREG (pos 0, len 2)
        tipoRegistro.PadRight(2).CopyTo(0, linha, 0, 2);
        // DATPRE (pos 2, len 8)
        dataPregao.PadRight(8).CopyTo(0, linha, 2, 8);
        // CODBDI (pos 10, len 2)
        codigoBDI.PadRight(2).CopyTo(0, linha, 10, 2);
        // CODNEG (pos 12, len 12)
        ticker.PadRight(12).CopyTo(0, linha, 12, 12);
        // TPMERC (pos 24, len 3)
        tipoMercado.PadRight(3).CopyTo(0, linha, 24, 3);
        // NOMRES (pos 27, len 12)
        nomeEmpresa.PadRight(12).CopyTo(0, linha, 27, 12);
        // PREABE (pos 56, len 13)
        precoAbertura.PadLeft(13, '0').CopyTo(0, linha, 56, 13);
        // PREMAX (pos 69, len 13)
        precoMaximo.PadLeft(13, '0').CopyTo(0, linha, 69, 13);
        // PREMIN (pos 82, len 13)
        precoMinimo.PadLeft(13, '0').CopyTo(0, linha, 82, 13);
        // PREMED (pos 95, len 13)
        precoMedio.PadLeft(13, '0').CopyTo(0, linha, 95, 13);
        // PREULT (pos 108, len 13)
        precoFechamento.PadLeft(13, '0').CopyTo(0, linha, 108, 13);
        // QUATOT (pos 152, len 18)
        quantidade.PadLeft(18, '0').CopyTo(0, linha, 152, 18);
        // VOLTOT (pos 170, len 18)
        volume.PadLeft(18, '0').CopyTo(0, linha, 170, 18);

        return new string(linha);
    }

    [Fact]
    public void ParseLinha_LinhaValida_RetornaCotacao()
    {
        var linha = CriarLinhaValida();

        var resultado = _parser.ParseLinha(linha);

        Assert.NotNull(resultado);
        Assert.Equal("PETR4", resultado.Ticker);
        Assert.Equal(new DateTime(2026, 2, 26), resultado.DataPregao);
        Assert.Equal("02", resultado.CodigoBDI);
        Assert.Equal(10, resultado.TipoMercado);
        Assert.Equal(38.50m, resultado.PrecoAbertura);
        Assert.Equal(39.50m, resultado.PrecoMaximo);
        Assert.Equal(37.50m, resultado.PrecoMinimo);
        Assert.Equal(38.50m, resultado.PrecoMedio);
        Assert.Equal(39.00m, resultado.PrecoFechamento);
        Assert.Equal(1000, resultado.QuantidadeNegociada);
        Assert.Equal(38500.00m, resultado.VolumeNegociado);
    }

    [Fact]
    public void ParseLinha_MercadoFracionario_RetornaCotacao()
    {
        var linha = CriarLinhaValida(
            codigoBDI: "96",
            ticker: "PETR4F",
            tipoMercado: "020");

        var resultado = _parser.ParseLinha(linha);

        Assert.NotNull(resultado);
        Assert.Equal("PETR4F", resultado.Ticker);
        Assert.Equal(20, resultado.TipoMercado);
        Assert.True(resultado.IsMercadoFracionario());
    }

    [Fact]
    public void ParseLinha_Header_RetornaNull()
    {
        var linha = CriarLinhaValida(tipoRegistro: "00");

        var resultado = _parser.ParseLinha(linha);

        Assert.Null(resultado);
    }

    [Fact]
    public void ParseLinha_Trailer_RetornaNull()
    {
        var linha = CriarLinhaValida(tipoRegistro: "99");

        var resultado = _parser.ParseLinha(linha);

        Assert.Null(resultado);
    }

    [Fact]
    public void ParseLinha_CodigoBDIInvalido_RetornaNull()
    {
        var linha = CriarLinhaValida(codigoBDI: "78");

        var resultado = _parser.ParseLinha(linha);

        Assert.Null(resultado);
    }

    [Fact]
    public void ParseLinha_TipoMercadoInvalido_RetornaNull()
    {
        var linha = CriarLinhaValida(tipoMercado: "070");

        var resultado = _parser.ParseLinha(linha);

        Assert.Null(resultado);
    }

    [Fact]
    public void ParseLinha_LinhaCurta_RetornaNull()
    {
        var resultado = _parser.ParseLinha("01202602260212345678901");

        Assert.Null(resultado);
    }

    [Fact]
    public void ParseLinha_LinhaVazia_RetornaNull()
    {
        Assert.Null(_parser.ParseLinha(""));
        Assert.Null(_parser.ParseLinha(null!));
    }

    [Fact]
    public void ParseLinha_PrecoConvertidoCorretamente()
    {
        // Preço: 0000000012345 => 123.45
        var linha = CriarLinhaValida(precoFechamento: "0000000012345");

        var resultado = _parser.ParseLinha(linha);

        Assert.NotNull(resultado);
        Assert.Equal(123.45m, resultado.PrecoFechamento);
    }

    [Fact]
    public void ParseLinha_DataInvalida_RetornaNull()
    {
        var linha = CriarLinhaValida(dataPregao: "99999999");

        var resultado = _parser.ParseLinha(linha);

        Assert.Null(resultado);
    }

    [Fact]
    public void ParseLinha_MercadoAVista_IsMercadoAVistaTrue()
    {
        var linha = CriarLinhaValida(tipoMercado: "010");

        var resultado = _parser.ParseLinha(linha);

        Assert.NotNull(resultado);
        Assert.True(resultado.IsMercadoAVista());
        Assert.False(resultado.IsMercadoFracionario());
    }
}
