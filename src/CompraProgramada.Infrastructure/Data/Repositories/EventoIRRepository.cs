using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Data.Repositories;

public class EventoIRRepository : IEventoIRRepository
{
    private readonly CompraProgramadaDbContext _context;

    public EventoIRRepository(CompraProgramadaDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(EventoIR evento)
    {
        await _context.EventosIR.AddAsync(evento);
    }

    public Task AtualizarAsync(EventoIR evento)
    {
        _context.EventosIR.Update(evento);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<EventoIR>> ObterNaoPublicadosAsync()
    {
        return await _context.EventosIR
            .Where(e => !e.PublicadoKafka)
            .OrderBy(e => e.DataEvento)
            .ToListAsync();
    }
}
