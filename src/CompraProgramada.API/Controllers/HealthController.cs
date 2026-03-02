using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.API.Controllers;

/// <summary>
/// Verificação de saúde da API.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Retorna o status de saúde da API.
    /// </summary>
    /// <returns>Status, timestamp e nome do serviço.</returns>
    /// <response code="200">API está saudável e operacional.</response>
    [HttpGet]
    [ProducesResponseType(200)]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Service = "Compra Programada API"
        });
    }
}
