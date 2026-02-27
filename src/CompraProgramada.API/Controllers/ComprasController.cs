using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers;

[ApiController]
[Route("api/[controller]")]
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
    [HttpPost("executar")]
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
    /// RN-020: Retorna as 3 datas de compra de um mês (dias 5, 15, 25 ajustados).
    /// </summary>
    [HttpGet("datas/{ano:int}/{mes:int}")]
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
