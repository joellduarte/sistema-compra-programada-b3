using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly ICestaService _cestaService;

    public AdminController(ICestaService cestaService)
    {
        _cestaService = cestaService;
    }

    /// <summary>
    /// RN-014 a RN-018: Cria nova cesta Top Five (desativa a anterior automaticamente).
    /// </summary>
    [HttpPost("cestas")]
    public async Task<IActionResult> CriarCesta([FromBody] CriarCestaRequest request)
    {
        try
        {
            var result = await _cestaService.CriarCestaAsync(request);
            return CreatedAtAction(nameof(ObterCestaPorId), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }

    /// <summary>
    /// Retorna a cesta Top Five atualmente ativa.
    /// </summary>
    [HttpGet("cestas/ativa")]
    public async Task<IActionResult> ObterCestaAtiva()
    {
        var result = await _cestaService.ObterCestaAtivaAsync();
        if (result is null)
            return NotFound(new { erro = "Nenhuma cesta ativa encontrada." });

        return Ok(result);
    }

    /// <summary>
    /// Retorna uma cesta por ID (ativa ou histórica).
    /// </summary>
    [HttpGet("cestas/{id:long}")]
    public async Task<IActionResult> ObterCestaPorId(long id)
    {
        var result = await _cestaService.ObterCestaPorIdAsync(id);
        if (result is null)
            return NotFound(new { erro = "Cesta não encontrada." });

        return Ok(result);
    }

    /// <summary>
    /// Retorna o histórico de todas as cestas (ativas e desativadas).
    /// </summary>
    [HttpGet("cestas/historico")]
    public async Task<IActionResult> ObterHistorico()
    {
        var result = await _cestaService.ObterHistoricoAsync();
        return Ok(result);
    }
}
