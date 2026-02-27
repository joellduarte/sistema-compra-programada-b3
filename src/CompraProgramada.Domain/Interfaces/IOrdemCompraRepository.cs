using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Domain.Interfaces;

public interface IOrdemCompraRepository
{
    Task AdicionarAsync(OrdemCompra ordem);
    Task<bool> ExisteParaDataAsync(DateTime dataReferencia);
}
