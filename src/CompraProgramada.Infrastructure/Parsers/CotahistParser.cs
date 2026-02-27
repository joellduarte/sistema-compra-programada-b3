using System.Globalization;
using System.Text;
using CompraProgramada.Application.Interfaces;
using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Infrastructure.Parsers;

public class CotahistParser : ICotahistParser
{
    // Posições 1-based da documentação B3, convertidas para 0-based no Substring
    // TIPREG:  1-2   (0, 2)
    // DATPRE:  3-10  (2, 8)
    // CODBDI: 11-12  (10, 2)
    // CODNEG: 13-24  (12, 12)
    // TPMERC: 25-27  (24, 3)
    // NOMRES: 28-39  (27, 12)
    // PREABE: 57-69  (56, 13)
    // PREMAX: 70-82  (69, 13)
    // PREMIN: 83-95  (82, 13)
    // PREMED: 96-108 (95, 13)
    // PREULT: 109-121(108, 13)
    // QUATOT: 153-170(152, 18)
    // VOLTOT: 171-188(170, 18)

    private static readonly HashSet<string> CodigosBDIValidos = ["02", "96"];
    private static readonly HashSet<int> TiposMercadoValidos = [10, 20];

    /// <summary>
    /// Faz parse de um arquivo COTAHIST da B3.
    /// Retorna apenas registros de detalhe (TIPREG=01) do mercado à vista (010) e fracionário (020).
    /// </summary>
    public IReadOnlyList<Cotacao> ParseArquivo(string caminhoArquivo)
    {
        if (!File.Exists(caminhoArquivo))
            throw new FileNotFoundException($"Arquivo COTAHIST não encontrado: {caminhoArquivo}");

        using var stream = File.OpenRead(caminhoArquivo);
        return ParseStream(stream);
    }

    public IReadOnlyList<Cotacao> ParseStream(Stream stream)
    {
        // Encoding ISO-8859-1 (Latin1) conforme documentação da B3
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding("ISO-8859-1");

        var cotacoes = new List<Cotacao>();
        using var reader = new StreamReader(stream, encoding);

        while (reader.ReadLine() is { } linha)
        {
            var cotacao = ParseLinha(linha);
            if (cotacao is not null)
                cotacoes.Add(cotacao);
        }

        return cotacoes;
    }

    /// <summary>
    /// Faz parse de uma única linha do arquivo COTAHIST.
    /// Retorna null se a linha não for um registro de detalhe válido.
    /// </summary>
    public Cotacao? ParseLinha(string linha)
    {
        if (string.IsNullOrEmpty(linha) || linha.Length < 245)
            return null;

        // Ignorar header (00) e trailer (99) — apenas detalhe (01)
        var tipoRegistro = linha[..2];
        if (tipoRegistro != "01")
            return null;

        // Filtrar por código BDI: 02 (lote padrão) e 96 (fracionário)
        var codigoBDI = linha.Substring(10, 2).Trim();
        if (!CodigosBDIValidos.Contains(codigoBDI))
            return null;

        // Filtrar por tipo de mercado: 010 (à vista) e 020 (fracionário)
        if (!int.TryParse(linha.Substring(24, 3).Trim(), out var tipoMercado))
            return null;
        if (!TiposMercadoValidos.Contains(tipoMercado))
            return null;

        // Parse da data do pregão (AAAAMMDD)
        var dataPregaoStr = linha.Substring(2, 8);
        if (!DateTime.TryParseExact(dataPregaoStr, "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dataPregao))
            return null;

        var ticker = linha.Substring(12, 12).Trim();
        var nomeEmpresa = linha.Substring(27, 12).Trim();

        // Preços (dividir por 100 — 2 casas decimais implícitas)
        var precoAbertura = ParsePreco(linha.Substring(56, 13));
        var precoMaximo = ParsePreco(linha.Substring(69, 13));
        var precoMinimo = ParsePreco(linha.Substring(82, 13));
        var precoMedio = ParsePreco(linha.Substring(95, 13));
        var precoFechamento = ParsePreco(linha.Substring(108, 13));

        // Volume
        var quantidadeNegociada = ParseLong(linha.Substring(152, 18));
        var volumeNegociado = ParsePreco(linha.Substring(170, 18));

        return Cotacao.Criar(
            dataPregao,
            ticker,
            codigoBDI,
            tipoMercado,
            nomeEmpresa,
            precoAbertura,
            precoFechamento,
            precoMaximo,
            precoMinimo,
            precoMedio,
            quantidadeNegociada,
            volumeNegociado);
    }

    /// <summary>
    /// Converte valor inteiro do arquivo para decimal com 2 casas.
    /// Ex: "0000000003850" => 38.50m
    /// </summary>
    private static decimal ParsePreco(string valorBruto)
    {
        if (long.TryParse(valorBruto.Trim(), out var valor))
            return valor / 100m;
        return 0m;
    }

    private static long ParseLong(string valorBruto)
    {
        if (long.TryParse(valorBruto.Trim(), out var valor))
            return valor;
        return 0;
    }
}
