namespace CompraProgramada.Application.DTOs;

public record CestaResponse(
    long Id,
    string Nome,
    bool Ativa,
    DateTime DataCriacao,
    DateTime? DataDesativacao,
    List<ItemCestaResponse> Itens);

public record ItemCestaResponse(
    string Ticker,
    decimal Percentual);
