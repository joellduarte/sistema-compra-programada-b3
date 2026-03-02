using CompraProgramada.Application.DTOs;
using CompraProgramada.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers;

/// <summary>
/// Gestão de clientes do produto de Compra Programada.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
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
    /// <remarks>
    /// Cria um novo cliente com CPF validado (11 dígitos, algoritmo oficial),
    /// valor mensal mínimo de R$ 100,00 e cria automaticamente as contas gráficas
    /// master (consolidação) e filhote (custódia individual).
    /// </remarks>
    /// <param name="request">Dados do cliente: nome, CPF, email e valor mensal.</param>
    /// <response code="201">Cliente criado com sucesso.</response>
    /// <response code="400">CPF duplicado, inválido ou valor mensal abaixo do mínimo.</response>
    [HttpPost("adesao")]
    [ProducesResponseType(typeof(AdesaoResponse), 201)]
    [ProducesResponseType(400)]
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
    /// <remarks>
    /// Marca o cliente como inativo. A posição em custódia é mantida,
    /// mas o cliente não participa mais das compras programadas.
    /// </remarks>
    /// <param name="clienteId">ID do cliente.</param>
    /// <response code="200">Cliente desativado com sucesso.</response>
    /// <response code="404">Cliente não encontrado.</response>
    /// <response code="400">Cliente já está inativo.</response>
    [HttpPost("{clienteId}/saida")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
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
    /// RN-011 a RN-013: Altera o valor mensal do aporte.
    /// </summary>
    /// <remarks>
    /// O novo valor deve ser no mínimo R$ 100,00.
    /// A alteração afeta apenas as compras futuras.
    /// </remarks>
    /// <param name="clienteId">ID do cliente.</param>
    /// <param name="request">Novo valor mensal.</param>
    /// <response code="200">Valor alterado com sucesso.</response>
    /// <response code="404">Cliente não encontrado.</response>
    /// <response code="400">Cliente inativo ou valor abaixo do mínimo.</response>
    [HttpPut("{clienteId}/valor-mensal")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(400)]
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
    /// <remarks>
    /// Retorna todos os ativos em custódia do cliente com quantidade, preço médio,
    /// valor atual baseado na última cotação e a rentabilidade calculada.
    /// </remarks>
    /// <param name="clienteId">ID do cliente.</param>
    /// <response code="200">Carteira do cliente com posições atualizadas.</response>
    /// <response code="404">Cliente não encontrado.</response>
    [HttpGet("{clienteId}/carteira")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
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
