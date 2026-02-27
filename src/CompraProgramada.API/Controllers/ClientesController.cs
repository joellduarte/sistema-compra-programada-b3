using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClientesController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    /// <summary>
    /// RN-001 a RN-006: Adesão ao produto de compra programada.
    /// </summary>
    [HttpPost("adesao")]
    public async Task<IActionResult> Aderir([FromBody] AdesaoRequest request)
    {
        try
        {
            var response = await _clienteService.AderirAsync(request);
            return Created($"/api/clientes/{response.ClienteId}/carteira", response);
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("CLIENTE_CPF_DUPLICADO"))
        {
            return BadRequest(new { erro = "CPF já cadastrado no sistema.", codigo = "CLIENTE_CPF_DUPLICADO" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message, codigo = "VALOR_MENSAL_INVALIDO" });
        }
    }

    /// <summary>
    /// RN-007 a RN-009: Saída do produto (mantém posição em custódia).
    /// </summary>
    [HttpPost("{clienteId}/saida")]
    public async Task<IActionResult> Sair(long clienteId)
    {
        try
        {
            var response = await _clienteService.SairAsync(clienteId);
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { erro = "Cliente não encontrado.", codigo = "CLIENTE_NAO_ENCONTRADO" });
        }
        catch (InvalidOperationException)
        {
            return BadRequest(new { erro = "Cliente já está inativo.", codigo = "CLIENTE_JA_INATIVO" });
        }
    }

    /// <summary>
    /// RN-011 a RN-013: Altera o valor mensal do cliente.
    /// </summary>
    [HttpPut("{clienteId}/valor-mensal")]
    public async Task<IActionResult> AlterarValorMensal(
        long clienteId, [FromBody] AlterarValorMensalRequest request)
    {
        try
        {
            var response = await _clienteService.AlterarValorMensalAsync(clienteId, request);
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { erro = "Cliente não encontrado.", codigo = "CLIENTE_NAO_ENCONTRADO" });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("CLIENTE_JA_INATIVO"))
        {
            return BadRequest(new { erro = "Cliente já está inativo.", codigo = "CLIENTE_JA_INATIVO" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { erro = ex.Message, codigo = "VALOR_MENSAL_INVALIDO" });
        }
    }

    /// <summary>
    /// Consulta a carteira (custódia) do cliente com cotações atuais.
    /// </summary>
    [HttpGet("{clienteId}/carteira")]
    public async Task<IActionResult> ConsultarCarteira(long clienteId)
    {
        try
        {
            var response = await _clienteService.ConsultarCarteiraAsync(clienteId);
            return Ok(response);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { erro = "Cliente não encontrado.", codigo = "CLIENTE_NAO_ENCONTRADO" });
        }
    }
}
