using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces;

public interface IEventoIRRepository
{
    Task AdicionarAsync(EventoIR evento);
    Task AtualizarAsync(EventoIR evento);
    Task<IReadOnlyList<EventoIR>> ObterNaoPublicadosAsync();
}
