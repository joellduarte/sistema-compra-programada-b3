using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Application.Interfaces;

public interface ICotahistParser
{
    IReadOnlyList<Cotacao> ParseArquivo(string caminhoArquivo);
    IReadOnlyList<Cotacao> ParseStream(Stream stream);
    Cotacao? ParseLinha(string linha);
}
