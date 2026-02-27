using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Application.Interfaces;

public interface ICotahistParser
{
    IReadOnlyList<Cotacao> ParseArquivo(string caminhoArquivo);
    Cotacao? ParseLinha(string linha);
}
