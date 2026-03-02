using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    [HttpPost("mudanca-cesta")]
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
    [HttpPost("desvio")]
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
