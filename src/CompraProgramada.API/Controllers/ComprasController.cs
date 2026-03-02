using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers;

/// <summary>
/// Motor de Compra Programada - execução de compras consolidadas.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ComprasController : ControllerBase
{
    private readonly IMotorCompraService _motorCompraService;

    public ComprasController(IMotorCompraService motorCompraService)
    {
        _motorCompraService = motorCompraService;
    }

    /// <summary>
    /// RN-020 a RN-044: Executa o ciclo de compra programada para uma data de referência.
    /// </summary>
    /// <remarks>
    /// Fluxo completo do motor:
    /// 1. Consolida aportes de todos os clientes ativos (1/3 do valor mensal por execução)
    /// 2. Compra na conta master (lote padrão ≥100 ações + fracionário 1-99 com sufixo F)
    /// 3. Distribui proporcionalmente para contas filhote com TRUNCAR (resíduos ficam na master)
    /// 4. Recalcula preço médio apenas em compras (vendas não alteram PM)
    /// 5. Registra IR dedo-duro (0,005%) e publica evento no Kafka
    ///
    /// As datas de compra são dias 5, 15 e 25 de cada mês (ajustados para próximo dia útil).
    /// </remarks>
    /// <param name="dataReferencia">Data de referência para a compra (formato: yyyy-MM-dd).</param>
    /// <response code="200">Compra executada com sucesso. Retorna detalhes de ordens, distribuições e totais.</response>
    /// <response code="400">Nenhum cliente ativo, cesta não configurada ou erro de processamento.</response>
    [HttpPost("executar")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ExecutarCompra([FromQuery] DateTime dataReferencia)
    {
        try
        {
            var result = await _motorCompraService.ExecutarCompraAsync(dataReferencia);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    /// <summary>
    /// RN-020: Retorna as 3 datas de compra de um mês (dias 5, 15, 25 ajustados para dia útil).
    /// </summary>
    /// <remarks>
    /// Se o dia 5, 15 ou 25 cair em sábado, a compra é movida para segunda-feira.
    /// Se cair em domingo, é movida para segunda-feira.
    /// </remarks>
    /// <param name="ano">Ano de referência.</param>
    /// <param name="mes">Mês de referência (1-12).</param>
    /// <response code="200">Lista com 3 datas de compra ajustadas.</response>
    [HttpGet("datas/{ano:int}/{mes:int}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public IActionResult ObterDatasCompra(int ano, int mes)
    {
        try
        {
            var diasOriginais = new[] { 5, 15, 25 };
            var datas = _motorCompraService.ObterDatasCompraMes(ano, mes);
            var cultura = new System.Globalization.CultureInfo("pt-BR");

            return Ok(new
            {
                ano,
                mes,
                datasCompra = datas.Select((d, i) => new
                {
                    dataOriginal = new DateTime(ano, mes, diasOriginais[i]).ToString("dd/MM/yyyy"),
                    dataExecucao = d.ToString("dd/MM/yyyy"),
                    diaSemana = d.ToString("dddd", cultura)
                })
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}
