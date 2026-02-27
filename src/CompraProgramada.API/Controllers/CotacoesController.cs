using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CotacoesController : ControllerBase
{
    private readonly ICotacaoService _cotacaoService;
    private readonly IConfiguration _configuration;

    public CotacoesController(ICotacaoService cotacaoService, IConfiguration configuration)
    {
        _cotacaoService = cotacaoService;
        _configuration = configuration;
    }

    /// <summary>
    /// Importa cotações de um arquivo COTAHIST da B3.
    /// O arquivo deve estar na pasta configurada em Cotacoes:CaminhoArquivos.
    /// </summary>
    [HttpPost("importar/{nomeArquivo}")]
    public async Task<IActionResult> ImportarArquivo(string nomeArquivo)
    {
        var pastaBase = _configuration["Cotacoes:CaminhoArquivos"] ?? "cotacoes";
        var caminhoCompleto = Path.Combine(pastaBase, nomeArquivo);

        if (!System.IO.File.Exists(caminhoCompleto))
            return NotFound(new { erro = $"Arquivo não encontrado: {nomeArquivo}" });

        var quantidade = await _cotacaoService.ImportarArquivoCotahistAsync(caminhoCompleto);

        return Ok(new
        {
            mensagem = $"Importação concluída com sucesso.",
            registrosImportados = quantidade,
            arquivo = nomeArquivo
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
