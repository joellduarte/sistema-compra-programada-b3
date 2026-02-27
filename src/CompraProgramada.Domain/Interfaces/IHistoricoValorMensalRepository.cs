using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces;

public interface IHistoricoValorMensalRepository
{
    Task AdicionarAsync(HistoricoValorMensal historico);
}
