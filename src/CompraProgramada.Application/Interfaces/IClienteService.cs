using CompraProgramada.Application.DTOs;

namespace CompraProgramada.Application.Interfaces;

public interface IClienteService
{
    Task<AdesaoResponse> AderirAsync(AdesaoRequest request);
    Task<SaidaResponse> SairAsync(long clienteId);
    Task<AlterarValorMensalResponse> AlterarValorMensalAsync(long clienteId, AlterarValorMensalRequest request);
    Task<CarteiraResponse> ConsultarCarteiraAsync(long clienteId);
    Task<RentabilidadeResponse> ConsultarRentabilidadeAsync(long clienteId);
}
