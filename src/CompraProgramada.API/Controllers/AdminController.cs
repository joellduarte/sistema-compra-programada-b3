using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers;

/// <summary>
/// Administração de Cestas Top Five (recomendação de ativos).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
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
    /// <remarks>
    /// A cesta deve conter exatamente 5 ativos (RN-014), a soma dos percentuais
    /// deve ser rigorosamente 100% (RN-015) e cada percentual deve ser maior que 0% (RN-016).
    /// Ao criar uma nova cesta, a anterior é desativada na mesma transação (RN-017/018).
    /// </remarks>
    /// <param name="request">Nome da cesta e lista de 5 itens com ticker e percentual.</param>
    /// <response code="201">Cesta criada e ativada com sucesso.</response>
    /// <response code="400">Validação falhou (quantidade de ativos, soma de percentuais, etc.).</response>
    [HttpPost("cestas")]
    [ProducesResponseType(typeof(CestaResponse), 201)]
    [ProducesResponseType(400)]
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
    /// <remarks>
    /// Sempre existe no máximo uma cesta ativa por vez.
    /// Esta é a cesta usada pelo Motor de Compra Programada.
    /// </remarks>
    /// <response code="200">Cesta ativa encontrada.</response>
    /// <response code="404">Nenhuma cesta ativa no momento.</response>
    [HttpGet("cestas/ativa")]
    [ProducesResponseType(typeof(CestaResponse), 200)]
    [ProducesResponseType(404)]
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
    /// <param name="id">ID da cesta.</param>
    /// <response code="200">Cesta encontrada.</response>
    /// <response code="404">Cesta não encontrada.</response>
    [HttpGet("cestas/{id:long}")]
    [ProducesResponseType(typeof(CestaResponse), 200)]
    [ProducesResponseType(404)]
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
    /// <response code="200">Lista de cestas ordenada por data de criação.</response>
    [HttpGet("cestas/historico")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ObterHistorico()
    {
        var result = await _cestaService.ObterHistoricoAsync();
        return Ok(result);
    }

    /// <summary>
    /// Consulta a custódia da conta master (resíduos de distribuição).
    /// </summary>
    /// <remarks>
    /// A conta master mantém ações residuais que não puderam ser distribuídas
    /// para as contas filhotes devido a arredondamentos (TRUNCAR).
    /// Esses resíduos são considerados na próxima compra programada (RN-039/040).
    /// </remarks>
    /// <response code="200">Custódia master com resíduos.</response>
    /// <response code="400">Conta master não encontrada.</response>
    [HttpGet("conta-master/custodia")]
    [ProducesResponseType(typeof(CustodiaMasterResponse), 200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ConsultarCustodiaMaster()
    {
        try
        {
            var result = await _cestaService.ConsultarCustodiaMasterAsync();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { erro = ex.Message });
        }
    }
}
