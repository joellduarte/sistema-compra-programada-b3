using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers;

/// <summary>
/// Gestão de cotações da B3 (importação COTAHIST e consultas).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CotacoesController : ControllerBase
{
    private readonly ICotacaoService _cotacaoService;

    public CotacoesController(ICotacaoService cotacaoService)
    {
        _cotacaoService = cotacaoService;
    }

    /// <summary>
    /// RN-055 a RN-062: Importa cotações via upload de arquivo COTAHIST (.TXT).
    /// </summary>
    /// <remarks>
    /// Aceita arquivos .TXT no layout posicional COTAHIST da B3 (245 caracteres por linha).
    /// Filtra apenas registros TIPREG=01 e mercados à vista (TPMERC 010/020).
    /// Preços são divididos por 100 conforme especificação da B3.
    /// Suporta arquivos anuais grandes (até 500MB) via streaming.
    /// </remarks>
    /// <param name="arquivo">Arquivo .TXT no formato COTAHIST da B3.</param>
    /// <response code="200">Importação concluída com sucesso.</response>
    /// <response code="400">Arquivo inválido ou nenhuma cotação encontrada.</response>
    [HttpPost("importar")]
    [RequestSizeLimit(500_000_000)]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ImportarArquivoUpload(IFormFile arquivo)
    {
        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new { erro = "Nenhum arquivo enviado." });

        var extensao = Path.GetExtension(arquivo.FileName)?.ToUpperInvariant();
        if (extensao != ".TXT")
            return BadRequest(new { erro = "Formato inválido. Envie um arquivo .TXT no formato COTAHIST da B3." });

        using var stream = arquivo.OpenReadStream();
        var quantidade = await _cotacaoService.ImportarStreamCotahistAsync(stream);

        if (quantidade == 0)
            return BadRequest(new { erro = "Nenhuma cotação válida encontrada. Verifique se o arquivo está no formato COTAHIST da B3." });

        return Ok(new
        {
            mensagem = "Importação concluída com sucesso.",
            registrosImportados = quantidade,
            arquivo = arquivo.FileName
        });
    }

    /// <summary>
    /// Obtém o preço de fechamento mais recente de um ticker.
    /// </summary>
    /// <param name="ticker">Código do ativo (ex: PETR4, VALE3, ITUB4).</param>
    /// <response code="200">Preço de fechamento encontrado.</response>
    /// <response code="404">Nenhuma cotação encontrada para o ticker informado.</response>
    [HttpGet("{ticker}/preco")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ObterPrecoFechamento(string ticker)
    {
        var preco = await _cotacaoService.ObterPrecoFechamentoAsync(ticker.ToUpperInvariant());

        if (preco is null)
            return NotFound(new { erro = $"Nenhuma cotação encontrada para {ticker}" });

        return Ok(new { ticker = ticker.ToUpperInvariant(), precoFechamento = preco });
    }

    /// <summary>
    /// Obtém todas as cotações de uma data de pregão.
    /// </summary>
    /// <param name="data">Data do pregão (formato: yyyy-MM-dd).</param>
    /// <response code="200">Lista de cotações do pregão.</response>
    /// <response code="404">Nenhuma cotação encontrada para a data.</response>
    [HttpGet("data/{data}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ObterPorData(DateTime data)
    {
        var cotacoes = await _cotacaoService.ObterCotacoesPorDataAsync(data);

        if (cotacoes.Count == 0)
            return NotFound(new { erro = $"Nenhuma cotação encontrada para {data:yyyy-MM-dd}" });

        return Ok(cotacoes.Select(c => new
        {
            c.Ticker,
            c.DataPregao,
            c.PrecoAbertura,
            c.PrecoFechamento,
            c.PrecoMaximo,
            c.PrecoMinimo,
            c.PrecoMedio,
            c.QuantidadeNegociada,
            c.VolumeNegociado,
            c.TipoMercado
        }));
    }

    /// <summary>
    /// Obtém a data do último pregão importado.
    /// </summary>
    /// <response code="200">Data do último pregão.</response>
    /// <response code="404">Nenhuma cotação importada ainda.</response>
    [HttpGet("ultimo-pregao")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ObterUltimaDataPregao()
    {
        var data = await _cotacaoService.ObterUltimaDataPregaoAsync();

        if (data is null)
            return NotFound(new { erro = "Nenhuma cotação importada ainda." });

        return Ok(new { ultimoPregao = data.Value.ToString("yyyy-MM-dd") });
    }
}
