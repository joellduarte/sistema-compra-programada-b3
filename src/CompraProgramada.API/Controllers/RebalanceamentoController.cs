using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers;

/// <summary>
/// Motor de Rebalanceamento de carteiras.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class RebalanceamentoController : ControllerBase
{
    private readonly IRebalanceamentoService _rebalanceamentoService;

    public RebalanceamentoController(IRebalanceamentoService rebalanceamentoService)
    {
        _rebalanceamentoService = rebalanceamentoService;
    }

    /// <summary>
    /// RN-045 a RN-049: Executa rebalanceamento por mudança de cesta.
    /// </summary>
    /// <remarks>
    /// Quando a cesta Top Five muda, este endpoint:
    /// 1. Identifica ativos que saíram, entraram e permaneceram (RN-046)
    /// 2. Vende toda a posição dos ativos que saíram (RN-047)
    /// 3. Compra novos ativos com o valor obtido das vendas (RN-048)
    /// 4. Rebalanceia ativos que permaneceram mas mudaram de percentual (RN-049)
    /// 5. Apura IR sobre vendas e publica eventos no Kafka (RN-057 a RN-062)
    /// </remarks>
    /// <param name="cestaAnteriorId">ID da cesta que estava ativa antes da mudança.</param>
    /// <param name="cestaNovaId">ID da nova cesta ativa.</param>
    /// <response code="200">Rebalanceamento executado. Retorna vendas, compras e apuração fiscal por cliente.</response>
    /// <response code="400">Cesta não encontrada ou erro de processamento.</response>
    [HttpPost("mudanca-cesta")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RebalancearPorMudancaCesta(
        [FromQuery] long cestaAnteriorId, [FromQuery] long cestaNovaId)
    {
        try
        {
            var result = await _rebalanceamentoService
                .RebalancearPorMudancaCestaAsync(cestaAnteriorId, cestaNovaId);
            return Ok(result);
        }
        catch (Exception ex) when (ex is KeyNotFoundException or InvalidOperationException)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    /// <summary>
    /// RN-050 a RN-052: Executa rebalanceamento por desvio de proporção.
    /// </summary>
    /// <remarks>
    /// Verifica se algum ativo desviou do percentual-alvo da cesta além do limiar configurado.
    /// Se houver desvio, vende ativos sobre-alocados e compra sub-alocados para
    /// reequilibrar a carteira conforme a composição da cesta ativa.
    /// Também apura IR sobre vendas realizadas (RN-057 a RN-062).
    /// </remarks>
    /// <param name="limiar">Limiar de desvio em pontos percentuais (padrão: 5pp).</param>
    /// <response code="200">Rebalanceamento executado. Retorna detalhes por cliente.</response>
    /// <response code="400">Nenhuma cesta ativa encontrada.</response>
    [HttpPost("desvio")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RebalancearPorDesvio(
        [FromQuery] decimal limiar = 5m)
    {
        try
        {
            var result = await _rebalanceamentoService.RebalancearPorDesvioAsync(limiar);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}
