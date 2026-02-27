using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CotacoesController : ControllerBase
{
    private readonly ICotacaoService _cotacaoService;

    public CotacoesController(ICotacaoService cotacaoService)
    {
        _cotacaoService = cotacaoService;
    }

    /// <summary>
    /// Importa cotações via upload de arquivo COTAHIST (.TXT).
    /// Aceita apenas arquivos .TXT com formato COTAHIST válido da B3.
    /// </summary>
    [HttpPost("importar")]
    [RequestSizeLimit(500_000_000)] // 500MB para arquivos anuais grandes
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
    [HttpGet("{ticker}/preco")]
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
    [HttpGet("data/{data}")]
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
    [HttpGet("ultimo-pregao")]
    public async Task<IActionResult> ObterUltimaDataPregao()
    {
        var data = await _cotacaoService.ObterUltimaDataPregaoAsync();

        if (data is null)
            return NotFound(new { erro = "Nenhuma cotação importada ainda." });

        return Ok(new { ultimoPregao = data.Value.ToString("yyyy-MM-dd") });
    }
}
